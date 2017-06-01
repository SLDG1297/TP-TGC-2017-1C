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

namespace TGC.Group.Model
{
    public class Shooter : TgcExample
    {
        private const int FACTOR = 8;
        // Constantes de escenario
        private const float MAP_SCALE_XZ = 160.0f; // Original = 20
        private const float MAP_SCALE_Y = 10.4f; // Original = 1.3

        // Menu
        private Menu menu;

		// Para saber si el juego esta inicializado
		private bool gameLoaded;

		// Tamanio de pantalla
		private Size windowSize;
     
        //skybox
        private TgcSkyBox skyBox;
        private TgcSimpleTerrain heightmap;

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
        private Surface g_pDepthStencil; // Depth-stencil buffer
        private Texture g_pRenderTarget, g_pRenderTarget4, g_pRenderTarget4Aux;
        private VertexBuffer g_pVBV3D;

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
        }

		public void InitGame()
		{
			// Iniciar jugador
			initJugador();
			// Iniciar HUD
			initText();

            // Iniciar escenario
            //initHeightmap();
            world.initWorld(MediaDir, heightmap);
			initSkyBox();

			var pmin = new Vector3(-16893, -2000, 17112);
			var pmax = new Vector3(18240, 8884, -18876);
			limits = new TgcBoundingAxisAlignBox(pmin, pmax);

			// Iniciar enemigos
			initEnemigos();

            // Iniciar bounding boxes
            world.initObstaculos();
            CollisionManager.Instance.setPlayer(jugador);

            // Iniciar cámara
            if (!FPSCamera) {
                // Configurar cámara en Tercera Persona y la asigno al TGC.
                camaraInterna = new ThirdPersonCamera(jugador, new Vector3(-40, 50, -50), 100, 150, Input);
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
				if (!FPSCamera)
				{
					// Update jugador
					jugador.mover(Input, posicionEnTerreno(jugador.Position.X, jugador.Position.Z), ElapsedTime);

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
					enemy.updateStatus(jugador.Position, ElapsedTime, obstaculos, posicionEnTerreno(enemy.Position.X, enemy.Position.Z));
				}

				//chequear colisiones con balas
				collisionManager.checkCollisions(ElapsedTime);
				// Update HUD
				updateText();
			}
        }

        public override void Render()
        {
            // Inicio el render de la escena, para ejemplos simples.
            // Cuando tenemos postprocesado o shaders es mejor realizar las operaciones según nuestra conveniencia.
            PreRender();            

            if (!gameLoaded)
			{
				menu.Render();
			}
			else
			{
	            // Render escenario
	            heightmap.render();
	            //limits.render();
	            if (!FPSCamera)
	            {
	                skyBox.render();

	            }
	            else
	            {
	                DrawText.drawText(Convert.ToString(Camara.Position), 10, 1000, Color.OrangeRed);
	            }

	            Utils.renderFromFrustum(world.Meshes, Frustum);

	            //TODO: Con QuadTree los FPS bajan. Tal vez sea porque 
	            //estan mas concentrados en una parte que en otra
	            //quadtree.render(Frustum, true);

	            // Render jugador
	            jugador.render(ElapsedTime);
	            
	            // Render enemigos
	            enemigos.ForEach(e => e.render(ElapsedTime));            

	            //renderizar balas y jugadores
	            collisionManager.renderAll(ElapsedTime);

	            // Render HUD
	            // DrawText.drawText("HEALTH: " + jugador.Health + "; BALAS: " + jugador.Arma.Balas + "; RECARGAS: " + jugador.Arma.Recargas, 50, 1000, Color.OrangeRed);
	            sombraTexto.render();
				texto.render();
			}
            // Finaliza el render y presenta en pantalla, al igual que el preRender se debe para casos puntuales es mejor utilizar a mano las operaciones de EndScene y PresentScene
            PostRender();
        }

        public override void Dispose()
        {
			if (!menu.GameStarted)
			{
				menu.Dispose();
				heightmap.dispose();
			}

			else
			{
                heightmap.dispose();
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
                var enemy_position_Y = posicionEnTerreno(enemy_position_X, enemy_position_Z);
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
            string heightmapDir = MediaDir + "Heightmaps\\heightmap.jpg";
            string textureDir = MediaDir + "Texturas\\map_v2.jpg";

            heightmap = new TgcSimpleTerrain();

            heightmap.loadHeightmap(heightmapDir, MAP_SCALE_XZ, MAP_SCALE_Y, Camara.LookAt);
            heightmap.loadTexture(textureDir);
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

        public float posicionEnTerreno(float x, float z)
        {
            // Da la posición del terreno en función del heightmap.
            int numeroMagico1 = 200;
            int numeroMagico2 = numeroMagico1 - 1;
            var largo = MAP_SCALE_XZ * numeroMagico1;
            var pos_i = numeroMagico1 * (0.5f + x / largo);
            var pos_j = numeroMagico1 * (0.5f + z / largo);

            var pi = (int)pos_i;
            var fracc_i = pos_i - pi;
            var pj = (int)pos_j;
            var fracc_j = pos_j - pj;

            if (pi < 0)
                pi = 0;
            else if (pi > numeroMagico2)
                pi = numeroMagico2;

            if (pj < 0)
                pj = 0;
            else if (pj > numeroMagico2)
                pj = numeroMagico2;

            var pi1 = pi + 1;
            var pj1 = pj + 1;
            if (pi1 > numeroMagico2)
                pi1 = numeroMagico2;
            if (pj1 > numeroMagico2)
                pj1 = numeroMagico2;

            var H0 = heightmap.HeightmapData[pi, pj] * MAP_SCALE_Y;
            var H1 = heightmap.HeightmapData[pi1, pj] * MAP_SCALE_Y;
            var H2 = heightmap.HeightmapData[pi, pj1] * MAP_SCALE_Y;
            var H3 = heightmap.HeightmapData[pi1, pj1] * MAP_SCALE_Y;
            var H = (H0 * (1 - fracc_i) + H1 * fracc_i) * (1 - fracc_j) +
                    (H2 * (1 - fracc_i) + H3 * fracc_i) * fracc_j;
            return H;
        }

        void corregirAltura(List<TgcMesh> meshes)
        {
            foreach (var mesh in meshes)
            {
                float posicionY = this.posicionEnTerreno(mesh.Position.X, mesh.Position.Z);
                mesh.Position = new Vector3(mesh.Position.X, posicionY, mesh.Position.Z);
                mesh.Transform = Matrix.Translation(0, posicionY, 0) * mesh.Transform;
            }
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
