using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Terrain;
using TGC.Core.SceneLoader;
using TGC.Core.Text;
using TGC.Core.Utils;
using TGC.Core.Geometry;
using TGC.Core.Textures;
using TGC.Core.Camara;
using TGC.Core.Collision;
using TGC.Core.BoundingVolumes;
using TGC.Group.Model.Cameras;
using TGC.Group.Model.Entities;
using TGC.Group.Model.Collisions;
using TGC.Group.Model.Optimization.Quadtree;
using TGC.Core.Shaders;
using Microsoft.DirectX.Direct3D;
using TGC.Core.Interpolation;
using System.Windows.Forms;
using TGC.Core.Sound;
using System.IO;
using TGC.Core.Input;
using TGC.Group.Model.UI;
using TGC.Group.Model.Optimization;

namespace TGC.Group.Model
{
    public class Shooter : TgcExample
    {
        private readonly float far_plane = 10000f;
        private readonly float near_plane = 1f;

        private readonly int SHADOWMAP_SIZE = 512;
        private const int FACTOR = 8;
        // Constantes de escenario
        // Menu
        private Menu menu;

		// Para saber si el juego esta inicializado
		private bool gameLoaded;

		// Tamanio de pantalla
		private Size windowSize;
     
        //skybox
        private TgcSkyBox skyBox;
        private Terreno terreno;

        //mundo con objetos
        private World world;

        // Bounding boxes del escenario
        private List<TgcBoundingAxisAlignBox> obstaculos = new List<TgcBoundingAxisAlignBox>();
        private TgcBoundingAxisAlignBox limits;

        // Cámara
        private ThirdPersonCamera camaraInterna;

        // Jugador
        private Player jugador;
        private Vector3 PLAYER_INIT_POS = new Vector3(800, 0, 1000);

        // Enemigos
        private List<Enemy> enemigos = new List<Enemy>();
		
        // HUD
		private TgcText2D texto = new TgcText2D();
		private TgcText2D sombraTexto = new TgcText2D();

        private UIManager UIManager;

        //otros
        private CollisionManager collisionManager;

		private bool FPSCamera = true;
        private Quadtree quadtree;

        //efectos
        private TgcTexture alarmTexture;
        private Effect gaussianBlur;
        private Effect alarmaEffect;
        private Surface depthStencil; // Depth-stencil buffer
        private Surface depthStencilOld;

        private Surface pOldRT;
        private Surface pOldDS;

        //vertex buffer de los triangulos
        private VertexBuffer screenQuadVB;
        //Render Targer sobre el cual se va a dibujar la pantalla
        private Texture renderTarget2D, g_pRenderTarget4, g_pRenderTarget4Aux;
        private InterpoladorVaiven intVaivenAlarm;

        private bool efecto = true;

        //variables para esconder el mouse
        public Point mouseCenter;
        public bool mouseEscondido;
        
        //sonidos
        public TgcStaticSound sound;

        //sombras
        private Effect shadowMap;
        private bool activateShadowMap = false;
        private float alfa_sol; // pos. del sol
        private Vector3 g_LightDir; // direccion de la luz actual
        private Vector3 g_LightPos; // posicion de la luz actual (la que estoy analizando)
        private Matrix g_LightView; // matriz de view del light
        private Matrix g_mShadowProj; // Projection matrix for shadow map
        private CubeTexture g_pCubeMapAgua;
        private Surface g_pDSShadow; // Depth-stencil buffer for rendering to shadow map

        private Texture g_pShadowMap; // Texture to which the shadow map is rendered


        /// <summary>
        ///     Constructor del juego.
        /// </summary>
        /// <param name="mediaDir">Ruta donde esta la carpeta con los assets</param>
        /// <param name="shadersDir">Ruta donde esta la carpeta con los shaders</param>
        public Shooter(string mediaDir, string shadersDir, Size windowSize) : base(mediaDir, shadersDir)
        {
            Category = Game.Default.Category;
            Name = Game.Default.Name;
            Description = Game.Default.Description;

			this.windowSize = windowSize;

			collisionManager = CollisionManager.Instance;
			menu = new Menu(MediaDir, windowSize);
        }

        public override void Init()
        {
			menu.Init();
            world = new World();
			initHeightmap();
			Camara = new MenuCamera(windowSize);

            loadPostProcessShaders();
        }

		public void InitGame()
		{
            var focusWindows = D3DDevice.Instance.Device.CreationParameters.FocusWindow;
            mouseCenter = focusWindows.PointToScreen(new Point(focusWindows.Width / 2, focusWindows.Height / 2));

            //Iniciar jugador
            initJugador();
			//Iniciar HUD
			initText();

            //Iniciar escenario
            //initHeightmap();
            world.initWorld(MediaDir,ShadersDir, terreno);
			initSkyBox();

			var pmin = new Vector3(-16893, -2000, 17112);
			var pmax = new Vector3(18240, 8884, -18876);
			limits = new TgcBoundingAxisAlignBox(pmin, pmax);

			//Iniciar enemigos
			initEnemigos();

            //Iniciar bounding boxes
            world.initObstaculos();
            CollisionManager.Instance.setPlayer(jugador);

            foreach(var mesh in world.Meshes)
            {
                if(activateShadowMap) mesh.Effect = shadowMap;
                //mesh.Technique = "RenderScene";
            }

            // Iniciar cámara
            if (!FPSCamera) {
                // Configurar cámara en Tercera Persona y la asigno al TGC.
                camaraInterna = new ThirdPersonCamera(jugador, new Vector3(-40, 50, -50), 60, 180, Input);
                Camara = camaraInterna;
            }
            else {
                // Antigua cámara en primera persona.
                Camara = new FirstPersonCamera(new Vector3(4000, 1500, 500), Input);
            }

            //quadtree = new Quadtree();
            //quadtree.create(world.Meshes, limits);
            //quadtree.createDebugQuadtreeMeshes();
            gameLoaded = true;
            loadPostProcessShaders();

            //escondo el cursor
            mouseEscondido = true;
            Cursor.Hide();

            if(activateShadowMap) createShadowMap();
            //SoundPlayer.Instance.playMusic(MediaDir, DirectSound);
            SoundPlayer.Instance.initAndPlayMusic(MediaDir, DirectSound, jugador);

            UIManager = new UIManager();
            UIManager.Init(MediaDir);
        }

        public void loadPostProcessShaders()
        {
            shadowMap =TgcShaders.loadEffect(ShadersDir + "ShadowMap.fx");

            var device = D3DDevice.Instance.Device;
            //Se crean 2 triangulos (o Quad) con las dimensiones de la pantalla con sus posiciones ya transformadas
            // x = -1 es el extremo izquiedo de la pantalla, x = 1 es el extremo derecho
            // Lo mismo para la Y con arriba y abajo
            // la Z en 1 simpre
            CustomVertex.PositionTextured[] screenQuadVertices =
            {
                new CustomVertex.PositionTextured(-1, 1, 1, 0, 0),
                new CustomVertex.PositionTextured(1, 1, 1, 1, 0),
                new CustomVertex.PositionTextured(-1, -1, 1, 0, 1),
                new CustomVertex.PositionTextured(1, -1, 1, 1, 1)
            };

            //vertex buffer de los triangulos
            screenQuadVB = new VertexBuffer(typeof(CustomVertex.PositionTextured),
                4, D3DDevice.Instance.Device, Usage.Dynamic | Usage.WriteOnly,
                CustomVertex.PositionTextured.Format, Pool.Default);
            screenQuadVB.SetData(screenQuadVertices, 0, LockFlags.None);

            //inicializo render target
            renderTarget2D = new Texture(device,
                device.PresentationParameters.BackBufferWidth, 
                device.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget,
                Format.X8R8G8B8, Pool.Default);

			g_pRenderTarget4 = new Texture(device, device.PresentationParameters.BackBufferWidth / 4, 
                                           device.PresentationParameters.BackBufferHeight / 4, 1, Usage.RenderTarget,
                                           Format.X8R8G8B8, Pool.Default);

            g_pRenderTarget4Aux = new Texture(device, device.PresentationParameters.BackBufferWidth / 4,
                                             device.PresentationParameters.BackBufferHeight / 4, 1, Usage.RenderTarget,
                                             Format.X8R8G8B8, Pool.Default);

            //Creamos un DepthStencil que debe ser compatible con nuestra definicion de renderTarget2D.
            depthStencil =
                device.CreateDepthStencilSurface(
                    device.PresentationParameters.BackBufferWidth,
                    device.PresentationParameters.BackBufferHeight,
                    DepthFormat.D24S8, MultiSampleType.None, 0, true);
            depthStencilOld = device.DepthStencilSurface;

            //cargo los shaders
            alarmaEffect = TgcShaders.loadEffect(ShadersDir + "PostProcess\\PostProcess.fx");
            alarmaEffect.Technique = "AlarmaTechnique";

            gaussianBlur = TgcShaders.loadEffect(ShadersDir + "PostProcess\\GaussianBlur.fx");
            gaussianBlur.Technique = "DefaultTechnique";
            gaussianBlur.SetValue("g_RenderTarget", renderTarget2D);
            // Resolucion de pantalla
            gaussianBlur.SetValue("screen_dx", device.PresentationParameters.BackBufferWidth);
            gaussianBlur.SetValue("screen_dy", device.PresentationParameters.BackBufferHeight);

            //Cargar textura que se va a dibujar arriba de la escena del Render Target
            alarmTexture = TgcTexture.createTexture(D3DDevice.Instance.Device, MediaDir + "Texturas\\efecto_alarma.png");

            //Interpolador para efecto de variar la intensidad de la textura de alarma
            intVaivenAlarm = new InterpoladorVaiven();
            intVaivenAlarm.Min = 0;
            intVaivenAlarm.Max = 1;
            intVaivenAlarm.Speed = 5;
            intVaivenAlarm.reset();
        }

        public void createShadowMap()
        {
            //--------------------------------------------------------------------------------------
            // Creo el shadowmap.
            // Format.R32F
            // Format.X8R8G8B8
            g_pShadowMap = new Texture(D3DDevice.Instance.Device, SHADOWMAP_SIZE, SHADOWMAP_SIZE,
                1, Usage.RenderTarget, Format.R32F,
                Pool.Default);

            // tengo que crear un stencilbuffer para el shadowmap manualmente
            // para asegurarme que tenga la el mismo tamano que el shadowmap, y que no tenga
            // multisample, etc etc.
            g_pDSShadow = D3DDevice.Instance.Device.CreateDepthStencilSurface(SHADOWMAP_SIZE,
                SHADOWMAP_SIZE,
                DepthFormat.D24S8,
                MultiSampleType.None,
                0,
                true);
            // por ultimo necesito una matriz de proyeccion para el shadowmap, ya
            // que voy a dibujar desde el pto de vista de la luz.
            // El angulo tiene que ser mayor a 45 para que la sombra no falle en los extremos del cono de luz
            // de hecho, un valor mayor a 90 todavia es mejor, porque hasta con 90 grados es muy dificil
            // lograr que los objetos del borde generen sombras
            var aspectRatio = D3DDevice.Instance.AspectRatio;
            g_mShadowProj = Matrix.PerspectiveFovLH(Geometry.DegreeToRadian(130.0f),
                aspectRatio, near_plane, far_plane);
            D3DDevice.Instance.Device.Transform.Projection =
                Matrix.PerspectiveFovLH(Geometry.DegreeToRadian(45.0f),
                    aspectRatio, near_plane, far_plane);
            

        }

        public void loadSounds(string MediaDir, string soundPath)
        {
            string currentFile = null;

            var shootingPath = MediaDir + soundPath;

            if (currentFile == null || currentFile != shootingPath)
            {
                currentFile = shootingPath;

                //Borrar sonido anterior
                if (sound != null)
                {
                    sound.dispose();
                    sound = null;
                }
                //Cargar sonido
                sound = new TgcStaticSound();
                sound.loadSound(currentFile, DirectSound.DsDevice);
            }
        }

        public override void Update()
        {
			PreUpdate();
			if (!menu.GameStarted)
			{
				menu.Update(ElapsedTime, Input);
			}
			else if (!gameLoaded)
			{
                InitGame();
			}

			else
			{


                alfa_sol += ElapsedTime * Geometry.DegreeToRadian(1.0f);
                if (alfa_sol > 2.5)
                    alfa_sol = 1.5f;
                // animo la posicion del sol
                //g_LightPos = new Vector3(1500f * (float)Math.Cos(alfa_sol), 1500f * (float)Math.Sin(alfa_sol), 0f);
                g_LightPos = new Vector3(5000 * (float)Math.Cos(alfa_sol), 5000 * (float)Math.Sin(alfa_sol),
                    5000f);
                g_LightDir = -g_LightPos;
                g_LightDir.Normalize();

                if (!FPSCamera)
				{
                    // Update jugador
                    if (!jugador.Muerto)
                    {
                        jugador.mover(Input, ElapsedTime, terreno);

                        // updownRot -= Input.YposRelative * 0.05f;
                        camaraInterna.OffsetHeight += Input.YposRelative;
                        camaraInterna.rotateY(Input.XposRelative * 0.05f);
                        camaraInterna.TargetDisplacement *= camaraInterna.RotationY * ElapsedTime;
                        // Hacer que la camara siga al personaje en su nueva posicion
                        camaraInterna.Target = jugador.Position;

                        var forward = camaraInterna.OffsetForward - Input.WheelPos * 10;
                        if (forward > 10)
                        {
                            camaraInterna.OffsetForward -= Input.WheelPos * 10;
                        }

                        UIManager.Update(jugador, ElapsedTime);
                    }
                    else
                    {                      
                        collisionManager.getPlayers().Remove(jugador);
                    }
					// Update SkyBox
					// Cuando se quiera probar cámara en tercera persona
					skyBox.Center = jugador.Position;

				}
				else
				{
					skyBox.Center = Camara.Position;
				}

                SoundPlayer.Instance.playAmbientSound(ElapsedTime);


                var enemigosASacar = new List<Enemy>();
                // Update enemigos.
                foreach (var enemy in enemigos)
				{
                    enemy.mover(jugador.Position, obstaculos, ElapsedTime,  terreno.posicionEnTerreno(enemy.Position.X, enemy.Position.Z));
                    if (enemy.Muerto) enemigosASacar.Add(enemy);
                }

                foreach (var enemigo in enemigosASacar)
                {
                    enemigo.dispose();
                    enemigos.Remove(enemigo);
                    collisionManager.getPlayers().Remove(enemigo);
                }

                world.updateWorld(ElapsedTime);
                //chequear colisiones con balas
                collisionManager.checkCollisions(ElapsedTime);

                // Update HUD
                //TODO : Borrar - Es solo para ver la posicion
                updateText();
                
                //TODO: hacer que ESC sea pausar! u otro!
                if (Input.keyPressed(Microsoft.DirectX.DirectInput.Key.Escape))
                {
                    if(mouseEscondido) Cursor.Show();
                    mouseEscondido = false;
                }

                //SoundPlayer.Instance.playPlayerSounds(MediaDir, Input, jugador);           
                if (mouseEscondido && !jugador.Muerto) Cursor.Position = mouseCenter;
                else Cursor.Show();
            }            
        }
        
        public override void Render()
        {
            ClearTextures();

            if (gameLoaded) world.initRenderEnvMap(Frustum, ElapsedTime, Camara, skyBox);
            var device = D3DDevice.Instance.Device;
            if (seDebeActivarEfecto() && activateShadowMap) RenderShadowMap();
            D3DDevice.Instance.Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);

            //esto es renderizar todo como viene, sin efectos
            gaussianBlur.Technique = "DefaultTechnique";
            alarmaEffect.Technique = "DefaultTechnique";

            //Cargamos el Render Targer al cual se va a dibujar la escena 3D. Antes nos guardamos el surface original 
            //En vez de dibujar a la pantalla, dibujamos a un buffer auxiliar, nuestro Render Target.
            //p0ldRT : antiguo render target
            pOldRT = device.GetRenderTarget(0);
            var pSurf = renderTarget2D.GetSurfaceLevel(0);
            if (seDebeActivarEfecto())
            {
                device.SetRenderTarget(0, pSurf);
            }
            //poldDs : old depthstencil
            pOldDS = device.DepthStencilSurface;

            //if (seDebeActivarEfecto()) device.DepthStencilSurface = depthStencilOld;
			if (seDebeActivarEfecto()) device.DepthStencilSurface = depthStencil;

            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);

            
            //Dibujamos la escena comun, pero en vez de a la pantalla al Render Target
           // terreno.getTerrerno().Effect = shadowMap;
            //terreno.getTerrerno().Technique = "RenderScene";
        
            drawScene(device, ElapsedTime);

            //Liberar memoria de surface de Render Target
            pSurf.Dispose();

            if (seDebeActivarEfecto())
            {
				device.SetRenderTarget(0, pOldRT);
                darkening(device);
				grayscale(device, (float)(100 - jugador.Health) / 100);
                //drawGaussianBlur(device);
                if (jugador.Health <= 10) drawAlarm(device, ElapsedTime);
                if (jugador.Muerto) drawGaussianBlur(device);
            }

            device.BeginScene();
            RenderFPS();
            if (gameLoaded)
            {
                RenderAxis();
                sombraTexto.render();
                texto.render();

                if (FPSCamera)
                {
                    DrawText.drawText(Convert.ToString(Camara.Position), 10, 1000, Color.OrangeRed);
                }
                else
                {
                    UIManager.Render();
                }
            }
            device.EndScene();
            device.Present();
        }
        
        private bool seDebeActivarEfecto()
        {
            return gameLoaded && efecto ;
        }

        public void drawScene(Device device, float ElapsedTime)
        {
            //dibujo la escena al render target
            device.BeginScene();
            if (!gameLoaded)
            {
                menu.Render();
            }
            else
            {

                world.restoreEffect();
                var lista = world.Meshes;

                // Render escenario
                terreno.render();
                //limits.render();
                if (!FPSCamera) skyBox.render();
                
                D3DDevice.Instance.ParticlesEnabled = true;
                D3DDevice.Instance.EnableParticles();

                RenderUtils.renderFromFrustum(world.Meshes, Frustum);
                RenderUtils.renderFromFrustum(world.BarrilesExplosivos, Frustum, ElapsedTime);
                RenderUtils.renderFromFrustum(collisionManager.getPlayers(), Frustum,ElapsedTime);
                RenderUtils.renderFromFrustum(collisionManager.getBalas(), Frustum);
                
                world.endRenderLagos(Camara, Frustum);
                //TODO: Con QuadTree los FPS bajan. Tal vez sea porque 
                //estan mas concentrados en una parte que en otra
                //quadtree.render(Frustum, true);    

                //el renderizado de los bounding box es para testear!
                //collisionManager.renderBoundingBoxes(ElapsedTime);
            }
            device.EndScene();
        }

        public void drawGaussianBlur(Device device)
        {
            int pasadas = 2;
            
            var pSurf = g_pRenderTarget4.GetSurfaceLevel(0);
            device.SetRenderTarget(0, pSurf);
            device.BeginScene();

            gaussianBlur.Technique = "DownFilter4";
            device.VertexFormat = CustomVertex.PositionTextured.Format;
            device.SetStreamSource(0, screenQuadVB, 0);
            gaussianBlur.SetValue("g_RenderTarget", renderTarget2D);

            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            gaussianBlur.Begin(FX.None);
            gaussianBlur.BeginPass(0);
            device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            gaussianBlur.EndPass();
            gaussianBlur.End();
            pSurf.Dispose();
            device.DepthStencilSurface = pOldDS;
            device.EndScene();

            device.DepthStencilSurface = pOldDS;

            //pasadas de blur
            for (var P = 0; P < pasadas ; ++P)
            {

              // Gaussian blur Horizontal
              // -----------------------------------------------------
              pSurf = g_pRenderTarget4Aux.GetSurfaceLevel(0);
              device.SetRenderTarget(0, pSurf);
             // dibujo el quad pp dicho :
              device.BeginScene();

               gaussianBlur.Technique = "GaussianBlurSeparable";
               device.VertexFormat = CustomVertex.PositionTextured.Format;
               device.SetStreamSource(0, screenQuadVB, 0);
               gaussianBlur.SetValue("g_RenderTarget", g_pRenderTarget4);

               device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
               gaussianBlur.Begin(FX.None);
               gaussianBlur.BeginPass(0);
               device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
               gaussianBlur.EndPass();
               gaussianBlur.End();
               pSurf.Dispose();

               device.EndScene();

                if (P < pasadas - 1)
                {
                    pSurf = g_pRenderTarget4.GetSurfaceLevel(0);
                    device.SetRenderTarget(0, pSurf);
                    pSurf.Dispose();
                    device.BeginScene();
                }
                else
                    // Ultima pasada vertical va sobre la pantalla pp dicha
                    device.SetRenderTarget(0, pOldRT);

                    //  Gaussian blur Vertical
                    // ----
                    gaussianBlur.Technique = "GaussianBlurSeparable";
                    device.VertexFormat = CustomVertex.PositionTextured.Format;
                    device.SetStreamSource(0, screenQuadVB, 0);
                    gaussianBlur.SetValue("g_RenderTarget", g_pRenderTarget4Aux);

                    device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
                    gaussianBlur.Begin(FX.None);
                    gaussianBlur.BeginPass(1);
                    device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
                    gaussianBlur.EndPass();
                    gaussianBlur.End();

                    if (P < pasadas - 1)
                    {
                        device.EndScene();
                    }
                }       
        }
        
		public void drawAlarm(Device device, float elapsedTime)
        {
            //device.SetRenderTarget(0, pOldRT);
            device.DepthStencilSurface = depthStencilOld;
            //Arrancamos la escena
            device.BeginScene();
            device.VertexFormat = CustomVertex.PositionTextured.Format;
            device.SetStreamSource(0, screenQuadVB, 0);


            alarmaEffect.Technique = "AlarmaTechnique";
            //Cargamos parametros en el shader de Post-Procesado
            alarmaEffect.SetValue("render_target2D", renderTarget2D);
            alarmaEffect.SetValue("textura_alarma", alarmTexture.D3dTexture);
            alarmaEffect.SetValue("alarmaScaleFactor", intVaivenAlarm.update(elapsedTime));

            //Limiamos la pantalla y ejecutamos el render del shader
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            alarmaEffect.Begin(FX.None);
            alarmaEffect.BeginPass(0);
            device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            alarmaEffect.EndPass();
            alarmaEffect.End();

            device.EndScene();
        }

        public void darkening(Device device) {
            //device.SetRenderTarget(0, pOldRT);
            device.DepthStencilSurface = depthStencilOld;
            //Arrancamos la escena
            device.BeginScene();
            device.VertexFormat = CustomVertex.PositionTextured.Format;
            device.SetStreamSource(0, screenQuadVB, 0);


            //alarmaEffect.Technique = "AlarmaTechnique";
            alarmaEffect.Technique = "OscurecerTechnique";
            //Cargamos parametros en el shader de Post-Procesado
            alarmaEffect.SetValue("render_target2D", renderTarget2D);
            //Limiamos la pantalla y ejecutamos el render del shader
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            alarmaEffect.Begin(FX.None);
            alarmaEffect.BeginPass(0);
            device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            alarmaEffect.EndPass();
            alarmaEffect.End();

            device.EndScene();
        }

		public void grayscale(Device device, float intensity)
		{
			//device.SetRenderTarget(0, pOldRT);
			device.DepthStencilSurface = depthStencilOld;
			//Arrancamos la escena
			device.BeginScene();
			device.VertexFormat = CustomVertex.PositionTextured.Format;
			device.SetStreamSource(0, screenQuadVB, 0);


			alarmaEffect.Technique = "GrayscaleTechnique";
			//Cargamos parametros en el shader de Post-Procesado
			alarmaEffect.SetValue("render_target2D", renderTarget2D);
			alarmaEffect.SetValue("gray_intensity", intensity);
			//Limiamos la pantalla y ejecutamos el render del shader
			device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
			alarmaEffect.Begin(FX.None);
			alarmaEffect.BeginPass(0);
			device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
			alarmaEffect.EndPass();
			alarmaEffect.End();

			device.EndScene();
		}

        public void RenderShadowMap()
        {
            g_LightPos = new Vector3(5000, 800, 5000);
            g_LightDir = g_LightPos - new Vector3(0,0,0) ;
            g_LightDir.Normalize();
            //Doy posicion a la luz
            // Calculo la matriz de view de la luz
            shadowMap.SetValue("g_vLightPos", new Vector4(g_LightPos.X, g_LightPos.Y, g_LightPos.Z, 1));
            shadowMap.SetValue("g_vLightDir", new Vector4(g_LightDir.X, g_LightDir.Y, g_LightDir.Z, 1));
            g_LightView = Matrix.LookAtLH(g_LightPos, g_LightPos + g_LightDir, new Vector3(0, 0, 1));

            // inicializacion standard:
            shadowMap.SetValue("g_mProjLight", g_mShadowProj);
            shadowMap.SetValue("g_mViewLightProj", g_LightView * g_mShadowProj);

            // Primero genero el shadow map, para ello dibujo desde el pto de vista de luz
            // a una textura, con el VS y PS que generan un mapa de profundidades.
            var pOldRT = D3DDevice.Instance.Device.GetRenderTarget(0);
            var pShadowSurf = g_pShadowMap.GetSurfaceLevel(0);
            D3DDevice.Instance.Device.SetRenderTarget(0, pShadowSurf);
            var pOldDS = D3DDevice.Instance.Device.DepthStencilSurface;
            D3DDevice.Instance.Device.DepthStencilSurface = g_pDSShadow;
            D3DDevice.Instance.Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.White, 1.0f, 0);
            D3DDevice.Instance.Device.BeginScene();

            // Hago el render de la escena pp dicha
            // solo los objetos que proyectan sombras:
            //Renderizar terreno
           // terreno.getTerrerno().Effect = shadowMap;
            //erreno.getTerrerno().Technique = "RenderShadow";
            //terreno.render();
            // dibujo el wolrd
            foreach(var mesh in world.Meshes)
            {
                mesh.Effect = shadowMap;
                mesh.Technique = "RenderShadow";
            }
            world.renderWorld(Frustum);
            
            // el tanque
            // Seteo la tecnica: estoy generando la sombra o estoy dibujando la escena
            //mesh.Technique = "RenderShadow";
            //mesh.render();
            // Termino
            D3DDevice.Instance.Device.EndScene();
            //TextureLoader.Save("shadowmap.bmp", ImageFileFormat.Bmp, g_pShadowMap);

            // restuaro el render target y el stencil
            D3DDevice.Instance.Device.DepthStencilSurface = pOldDS;
            D3DDevice.Instance.Device.SetRenderTarget(0, pOldRT);

            shadowMap.SetValue("g_txShadow", g_pShadowMap);

            foreach (var mesh in world.Meshes)
            {
                mesh.Technique = "RenderScene";
            }
            world.restoreEffect();
        }

        public override void Dispose()
        {
            terreno.dispose();

            if (!menu.GameStarted)
            {
                menu.Dispose();
            }

            else
            {
                world.disposeWorld();
                // Dispose bounding boxes
                obstaculos.ForEach(o => o.dispose());

                limits.dispose();
                // Dispose jugador
                //jugador.dispose();

                // Dispose enemigos
                //enemigos.ForEach(e => e.dispose());
                collisionManager.disposeAll();

                // Dispose HUD
                UIManager.Dispose();
                texto.Dispose();
                sombraTexto.Dispose();
            }

            gaussianBlur.Dispose();
            alarmaEffect.Dispose();
            renderTarget2D.Dispose();
            g_pRenderTarget4Aux.Dispose();
            g_pRenderTarget4.Dispose();
            screenQuadVB.Dispose();
            depthStencil.Dispose();
            depthStencilOld.Dispose();
            
        }

#region Métodos Auxiliares
        private void initJugador()
        {
            jugador = new Player(MediaDir, "CS_Gign", PLAYER_INIT_POS, Arma.AK47(MediaDir));
        }

        private void initEnemigos()
        {
            var rndm = new Random();
            for (var i = 0; i < 15; i++)
            {
                var enemy_position_X = -rndm.Next(-1500 * FACTOR, 1500 * FACTOR);
                var enemy_position_Z = -rndm.Next(-1500 * FACTOR, 1500 * FACTOR);
                var enemy_position_Y = terreno.posicionEnTerreno(enemy_position_X, enemy_position_Z);
                var enemy_position = new Vector3(enemy_position_X, enemy_position_Y, enemy_position_Z);
                enemy_position = Vector3.TransformCoordinate(enemy_position, Matrix.RotationY(Utils.DegreeToRadian(rndm.Next(0, 360))));
                var enemigo = new Enemy(MediaDir, "CS_Arctic", enemy_position, Arma.AK47(MediaDir));
                if (!enemigo.isCollidingWithObject(obstaculos))
                {
                    enemigos.Add(enemigo);
                }
                else
                {
                    i--;
                }
            }

            foreach (var enemy in enemigos) collisionManager.addEnemy(enemy);
        }

        private void initHeightmap()
        {
            terreno = new Terreno(MediaDir, Camara.LookAt);
            collisionManager.setTerrain(terreno);
        }

        private void initSkyBox(){
            skyBox = new TgcSkyBox();
            skyBox.Center = jugador.Position;
            skyBox.Size = new Vector3(10000, 10000, 10000);

			string skyBoxDir = MediaDir + "Texturas\\Quake\\SkyBoxWhale\\Whale";

			skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Up, skyBoxDir + "up.jpg");
			skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Down, skyBoxDir + "dn.jpg");
			skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Left, skyBoxDir + "lf.jpg");
			skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Right, skyBoxDir + "rt.jpg");
			skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Front, skyBoxDir + "bk.jpg");
			skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Back, skyBoxDir + "ft.jpg");

            skyBox.Init();
        }	

		private void initText() {
			updateText();

            // Lo pongo arriba a la izquierda porque no sabemos el tamanio de pantalla
            texto.Color = Color.Maroon;
			texto.Position = new Point(50, 50);
			texto.Size = new Size(texto.Text.Length * 24, 24);
			texto.Align = TgcText2D.TextAlign.LEFT;

			var font = new System.Drawing.Text.PrivateFontCollection();
			font.AddFontFile(MediaDir + "Fonts\\pdark.ttf");
			texto.changeFont(new System.Drawing.Font(font.Families[0], 24, FontStyle.Bold));

			sombraTexto.Color = Color.DarkGray;
			sombraTexto.Position = new Point(53, 52);
			sombraTexto.Size = new Size(texto.Text.Length * 24, 24);
			sombraTexto.Align = TgcText2D.TextAlign.LEFT;
			sombraTexto.changeFont(new System.Drawing.Font(font.Families[0], 24, FontStyle.Bold));
		}

		private void updateText() {
			//texto.Text = "HEALTH: " + jugador.Health;
			//texto.Text += "\tBALAS: " + jugador.Arma.Balas;
			//texto.Text += "\tRECARGAS: " + jugador.Arma.Recargas;
            texto.Text = "\nPosition\n" + jugador.Position;

            sombraTexto.Text = texto.Text;
		}

		private void renderAABB() {
            // Del Jugador
            //jugador.Esqueleto.BoundingBox.render();
            //foreach (var roca in rocas) roca.BoundingBox.render();

            // De todos los obstáculos
            obstaculos.ForEach(o => o.render());
		}       

        TgcScene cargarScene(string unaDireccion)
        {
            return new TgcSceneLoader().loadSceneFromFile(unaDireccion);
        }

        TgcMesh cargarMesh(string unaDireccion)
        {
            return cargarScene(unaDireccion).Meshes[0];
        }

        void aniadirObstaculoAABB(List<TgcMesh> meshes)
        {
            foreach (var mesh in meshes)
            {
                //obstaculos.Add(mesh.BoundingBox);
                CollisionManager.Instance.agregarAABB(mesh.BoundingBox);
            }
        }

        void aniadirObstaculoAABB(List<Enemy> enemigos)
        {
            foreach (var enemigo in enemigos)
            {
                obstaculos.Add(enemigo.Esqueleto.BoundingBox);
            }
        }
        #endregion
    }
}
