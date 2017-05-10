using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Terrain;
using TGC.Core.SceneLoader;
using TGC.Core.Text;
using TGC.Group.Model.Cameras;
using System.Collections.Generic;
using TGC.Core.Utils;
using TGC.Core.Geometry;
using TGC.Core.Textures;
using TGC.Core.Collision;
using TGC.Core.BoundingVolumes;
using TGC.Group.Model.Entities;
using System.Drawing;
using TGC.Group.Model.Collisions;

namespace TGC.Group.Model
{
    public  class Shooter : TgcExample
    {
        // Constantes de escenario
        private const float MAP_SCALE_XZ = 160.0f; // Original = 20
        private const float MAP_SCALE_Y = 10.4f; // Original = 1.3
        private const int FACTOR = 8; // Significa las veces que se agrandó según el MAP_SCALE original.
        // Esto se hace así porque ya hay valores hardcodeados de posiciones que no quiero cambiar.
        // Habría que ver una forma de ubicar meshes en posición relativa en el espacio.
        private Vector3 CENTRO = new Vector3(0, 0, 0);
        

        // Escenario
        private TgcSimpleTerrain heightmap;

		private TgcSkyBox skyBox;

        private TgcScene casa;

        private TgcMesh rocaOriginal;
        private TgcMesh palmeraOriginal;
        private TgcMesh pastito;
        private TgcMesh faraon;
        private TgcMesh arbolSelvatico;

        private TgcBox cajita;

        private List<TgcMesh> rocas = new List<TgcMesh>();
        private List<TgcMesh> palmeras = new List<TgcMesh>();
        private List<TgcMesh> pastitos = new List<TgcMesh>();
        private List<TgcMesh> cajitas = new List<TgcMesh>();
        private List<TgcMesh> arbolesSelvaticos = new List<TgcMesh>();

        // Bounding boxes del escenario
        private List<TgcBoundingAxisAlignBox> obstaculos = new List<TgcBoundingAxisAlignBox>();

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

        /// <summary>
        ///     Constructor del juego.
        /// </summary>
        /// <param name="mediaDir">Ruta donde esta la carpeta con los assets</param>
        /// <param name="shadersDir">Ruta donde esta la carpeta con los shaders</param>
        public Shooter(string mediaDir, string shadersDir) : base(mediaDir, shadersDir)
        {
            Category = Game.Default.Category;
            Name = Game.Default.Name;
            Description = Game.Default.Description;

            collisionManager = CollisionManager.Instance;
        }

        public override void Init()
        {
            // Iniciar jugador
            initJugador();

            // Iniciar HUD
            initText();

            // Iniciar escenario
            initHeightmap();

            initSkyBox();

            initScene();

            // Iniciar enemigos
            initEnemigos();

            // Iniciar bounding boxes
            initObstaculos();

            // Iniciar cámara

            // Antigua cámara en primera persona.
            // Camara = new FirstPersonCamera(new Vector3(0, 1500, 0), Input);

            // Antigua cámara en tercera persona.
            // camaraInterna = new ThirdPersonCamera(jugador, 50, 150, Input);

            // Cámara actual en tercera persona.
            // Configurar cámara en Tercera Persona y la asigno al TGC.
            camaraInterna = new ThirdPersonCamera(jugador, new Vector3(-40,50,-50), 100, 150, Input);
            Camara = camaraInterna;
        }

        public override void Update()
        {
            PreUpdate();

            // Update jugador
            jugador.mover(Input, posicionEnTerreno(jugador.Position.X, jugador.Position.Z), ElapsedTime, obstaculos);

			// updownRot -= Input.YposRelative * 0.05f;
			camaraInterna.OffsetHeight += Input.YposRelative;
			camaraInterna.rotateY(Input.XposRelative * 0.05f);
			camaraInterna.TargetDisplacement *= camaraInterna.RotationY * ElapsedTime;
            // Hacer que la camara siga al personaje en su nueva posicion
            camaraInterna.Target = jugador.Position;

            var forward = camaraInterna.OffsetForward - Input.WheelPos * 10;
			if (forward > 10) {
				camaraInterna.OffsetForward -= Input.WheelPos * 10;
			}

			// Update enemigos.
			foreach (var enemy in enemigos)
			{
				enemy.updateStatus(jugador.Position, ElapsedTime, obstaculos, posicionEnTerreno(enemy.Position.X, enemy.Position.Z));
			}
            
            // Update SkyBox

            // Cuando se quiera probar cámara en primera persona
            // skyBox.Center = Camara.Position;

            // Cuando se quiera probar cámara en tercera persona
            skyBox.Center = jugador.Position;

            //chequear colisiones con balas
            collisionManager.checkCollisions(ElapsedTime);
            // Update HUD
            updateText();
        }

        public override void Render()
        {
            // Inicio el render de la escena, para ejemplos simples.
            // Cuando tenemos postprocesado o shaders es mejor realizar las operaciones según nuestra conveniencia.
            PreRender();

            // Render escenario
            heightmap.render();

            skyBox.render();

            casa.renderAll();

            Utils.renderMeshes(rocas);
            Utils.renderMeshes(palmeras);
            Utils.renderMeshes(pastitos);
            faraon.render();

            Utils.renderMeshes(cajitas);

            // Render jugador
            //jugador.render(ElapsedTime);

            // Render enemigos
            //enemigos.ForEach(e => e.render(ElapsedTime));

            arbolSelvatico.render();
            // Render bounding boxes
            //renderAABB();            

            //renderizar balas y jugadores
            collisionManager.renderAll(ElapsedTime);

            // Render HUD
            // DrawText.drawText("HEALTH: " + jugador.Health + "; BALAS: " + jugador.Arma.Balas + "; RECARGAS: " + jugador.Arma.Recargas, 50, 1000, Color.OrangeRed);
            sombraTexto.render();
			texto.render();       

            // Finaliza el render y presenta en pantalla, al igual que el preRender se debe para casos puntuales es mejor utilizar a mano las operaciones de EndScene y PresentScene
            PostRender();
        }

        public override void Dispose()
        {
            // Dispose escenario
            heightmap.dispose();
            skyBox.dispose();

            casa.disposeAll();

            rocaOriginal.dispose();
            palmeraOriginal.dispose();
            pastito.dispose();
            faraon.dispose();
            arbolSelvatico.dispose();
            cajita.dispose();

            // Dispose bounding boxes
            obstaculos.ForEach(o => o.dispose());

            // Dispose jugador
            //jugador.dispose();

            // Dispose enemigos
            //enemigos.ForEach(e => e.dispose());
            collisionManager.disposeAll();

            // Dispose HUD
			texto.Dispose();
			sombraTexto.Dispose();

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

        private void initHeightmap(){
            string heightmapDir = MediaDir + "Heightmaps\\heightmap.jpg";
            string textureDir = MediaDir + "Texturas\\map_v2.jpg";

            heightmap = new TgcSimpleTerrain();

            heightmap.loadHeightmap(heightmapDir, MAP_SCALE_XZ, MAP_SCALE_Y, Camara.LookAt);
            heightmap.loadTexture(textureDir);
        }

        private void initScene(){
            // Ubicación de la casa.
            string casaDir = MediaDir + "Meshes\\Edificios\\Casa\\Casa-TgcScene.xml";
            casa = cargarScene(casaDir);
            foreach (var mesh in casa.Meshes)
            {
                var position = new Vector3(-800 * FACTOR, 0, 1200 * FACTOR);
                mesh.Position = position;
                mesh.AutoTransformEnable = false;
                mesh.Transform = Matrix.Scaling(1.5f, 2f, 1.75f) * Matrix.RotationY(FastMath.PI_HALF + FastMath.PI) * Matrix.Translation(mesh.Position);
            }

            // Creación de rocas en línea circular y escaladas.
            string rocaDir = MediaDir + "Meshes\\Vegetation\\Roca\\Roca-TgcScene.xml";
            rocaOriginal = cargarMesh(rocaDir);
            Utils.disponerEnCirculoXZ(rocaOriginal, rocas, 4, 500 * FACTOR, FastMath.PI_HALF);
            foreach (var roca in rocas)
            {
                roca.AutoTransformEnable = false;
                roca.Scale = new Vector3(3 * FACTOR, 2 * FACTOR, 3 * FACTOR);

                roca.Transform = Matrix.Scaling(roca.Scale) * roca.Transform;
            }

            // Creación de palmeras dispuestas circularmente.
            string palmeraDir = MediaDir + "Meshes\\Vegetation\\Palmera\\Palmera-TgcScene.xml";
            palmeraOriginal = cargarMesh(palmeraDir);
            Utils.disponerEnCirculoXZ(palmeraOriginal, palmeras, 200, 820 * FACTOR, 1);
            foreach (var palmera in palmeras)
            {
                palmera.AutoTransformEnable = false;
                palmera.Scale = new Vector3(1.5f, 1.5f, 1.5f);
                palmera.Transform = Matrix.Scaling(palmera.Scale) * palmera.Transform;
            }

            // Creación de pastitos.
            string pastitoDir = MediaDir + "Meshes\\Vegetation\\Pasto\\Pasto-TgcScene.xml";
            pastito = cargarMesh(pastitoDir);

            // Creación de faraón.
            string faraonDir = MediaDir + "Meshes\\Objetos\\EstatuaFaraon\\EstatuaFaraon-TgcScene.xml";
            faraon = cargarMesh(faraonDir);
            faraon.AutoTransformEnable = false;
            //faraon.Scale = new Vector3(FACTOR, FACTOR, FACTOR);
            faraon.Transform = Matrix.Scaling(FACTOR, FACTOR, FACTOR) * faraon.Transform;
           
            // Creación de cajitas.
            cajita = TgcBox.fromSize(new Vector3(30 * FACTOR, 30 * FACTOR, 30 * FACTOR), TgcTexture.createTexture(MediaDir + "Texturas\\paja4.jpg"));
            Utils.disponerEnRectanguloXZ(cajita.toMesh("cajita"), cajitas, 2, 2, 50);
            foreach (var cajita in cajitas)
            {
                cajita.AutoTransformEnable = false;
                cajita.Scale = new Vector3(-800 * FACTOR, 20 * FACTOR, 1400 * FACTOR);
                cajita.Transform = Matrix.Scaling(0.25f,0.25f,0.25f) * Matrix.Translation(cajita.Scale) * cajita.Transform;
            }

            //creacion de arboles selvaticos
            string arbolSelvaticoDir = MediaDir + "Meshes\\Vegetation\\ArbolSelvatico\\ArbolSelvatico-TgcScene.xml";
            arbolSelvatico = cargarMesh(arbolSelvaticoDir);

            arbolSelvatico.Position = new Vector3(800, 0, 200);

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

		private void initObstaculos() {
            //Añadir escenario.
            aniadirObstaculoAABB(casa.Meshes);

            aniadirObstaculoAABB(rocas);
            //aniadirObstaculoAABB(palmeras);

            foreach (var palmera in palmeras)
            {
                var despl = new Vector3(0, 100, 0);
                var cilindro = new TgcBoundingCylinderFixedY(
                    palmera.Position + despl, 20, 100);

                collisionManager.agregarCylinder(cilindro);
            }

            //aniadirObstaculoAABB(pastitos);
            aniadirObstaculoAABB(cajitas);

            // Añadir enemigos.
            //aniadirObstaculoAABB(enemigos);

            var cylinder = new TgcBoundingCylinderFixedY(arbolSelvatico.BoundingBox.calculateBoxCenter() + new Vector3(500,0,45),
                60, 200);
            CollisionManager.Instance.agregarCylinder(cylinder);            
            CollisionManager.Instance.setPlayer(jugador);
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
			texto.changeFont(new Font(font.Families[0], 24, FontStyle.Bold));

			sombraTexto.Color = Color.DarkGray;
			sombraTexto.Position = new Point(53, 52);
			sombraTexto.Size = new Size(texto.Text.Length * 24, 24);
			sombraTexto.Align = TgcText2D.TextAlign.LEFT;
			sombraTexto.changeFont(new Font(font.Families[0], 24, FontStyle.Bold));
		}

		private void updateText() {
			texto.Text = "HEALTH: " + jugador.Health;
			texto.Text += "\tBALAS: " + jugador.Arma.Balas;
			texto.Text += "\tRECARGAS: " + jugador.Arma.Recargas;
            texto.Text += " Position: " + jugador.Position;

            sombraTexto.Text = texto.Text;
		}

		private void renderAABB() {
            // Del Jugador
            //jugador.Esqueleto.BoundingBox.render();
            foreach (var roca in rocas) roca.BoundingBox.render();

            // De todos los obstáculos
            obstaculos.ForEach(o => o.render());
		}

		public float posicionEnTerreno(float x, float z)
        {
            // Da la posición del terreno en función del heightmap.
            var largo = MAP_SCALE_XZ * 200;
            var pos_i = 200f * (0.5f + x / largo);
            var pos_j = 200f * (0.5f + z / largo);

            var pi = (int)pos_i;
            var fracc_i = pos_i - pi;
            var pj = (int)pos_j;
            var fracc_j = pos_j - pj;

            if (pi < 0)
                pi = 0;
            else if (pi > 199)
                pi = 199;

            if (pj < 0)
                pj = 0;
            else if (pj > 199)
                pj = 199;

            var pi1 = pi + 1;
            var pj1 = pj + 1;
            if (pi1 > 199)
                pi1 = 199;
            if (pj1 > 199)
                pj1 = 199;

            var H0 = heightmap.HeightmapData[pi, pj] * MAP_SCALE_Y;
            var H1 = heightmap.HeightmapData[pi1, pj] * MAP_SCALE_Y;
            var H2 = heightmap.HeightmapData[pi, pj1] * MAP_SCALE_Y;
            var H3 = heightmap.HeightmapData[pi1, pj1] * MAP_SCALE_Y;
            var H = (H0 * (1 - fracc_i) + H1 * fracc_i) * (1 - fracc_j) +
                    (H2 * (1 - fracc_i) + H3 * fracc_i) * fracc_j;
            return H;
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
