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

namespace TGC.Group.Model
{
    public class Shooter : TgcExample
    {
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

        private bool efecto = false;

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

            // Iniciar cámara
            if (!FPSCamera) {
                // Configurar cámara en Tercera Persona y la asigno al TGC.
                camaraInterna = new ThirdPersonCamera(jugador, new Vector3(-40, 50, -50), 50, 150, Input);
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
		}

        public void loadPostProcessShaders()
        {
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
                world.updateWorld(ElapsedTime);
				if (!FPSCamera)
				{
					// Update jugador
					jugador.mover(Input, terreno.posicionEnTerreno(jugador.Position.X, jugador.Position.Z), ElapsedTime);

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

					// Update SkyBox
					// Cuando se quiera probar cámara en tercera persona
					skyBox.Center = jugador.Position;
				}
				else
				{
					skyBox.Center = Camara.Position;
				}

				// Update enemigos.
				foreach (var enemy in enemigos)
				{
					enemy.updateStatus(jugador.Position, ElapsedTime, obstaculos, terreno.posicionEnTerreno(enemy.Position.X, enemy.Position.Z));
				}

				//chequear colisiones con balas
				collisionManager.checkCollisions(ElapsedTime);
				// Update HUD
				updateText();
			}            
        }
        
        public override void Render()
        {
            ClearTextures();
            var device = D3DDevice.Instance.Device;

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

            if (seDebeActivarEfecto()) device.DepthStencilSurface = depthStencilOld;

            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);

            //Dibujamos la escena comun, pero en vez de a la pantalla al Render Target
            drawScene(device, ElapsedTime);

            //Liberar memoria de surface de Render Target
            pSurf.Dispose();

            if (seDebeActivarEfecto())
            {
                drawGaussianBlur(device);
                //if(jugador.Health < 20) drawAlarm(device, ElapsedTime);
            }

            device.BeginScene();
            RenderFPS();
            if (gameLoaded)
            {
                RenderAxis();
                sombraTexto.render();
                texto.render();

                if(FPSCamera) DrawText.drawText(Convert.ToString(Camara.Position), 10, 1000, Color.OrangeRed);
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
                var lista = world.Meshes;

                // Render escenario
                terreno.render();
                //limits.render();
                if (!FPSCamera) skyBox.render();         

                Utils.renderFromFrustum(world.Meshes, Frustum);
                Utils.renderFromFrustum(collisionManager.getPlayers(), Frustum,ElapsedTime);
                //TODO: Con QuadTree los FPS bajan. Tal vez sea porque 
                //estan mas concentrados en una parte que en otra
                //quadtree.render(Frustum, true);    

                //renderizar balas
                collisionManager.renderAll(ElapsedTime);
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
            device.SetRenderTarget(0, pOldRT);
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
			texto.Text = "HEALTH: " + jugador.Health;
			texto.Text += "\tBALAS: " + jugador.Arma.Balas;
			texto.Text += "\tRECARGAS: " + jugador.Arma.Recargas;
            texto.Text += "\nPosition\n" + jugador.Position;

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
