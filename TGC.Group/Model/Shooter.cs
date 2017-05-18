using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
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

namespace TGC.Group.Model
{
    public class Shooter : TgcExample
    {
        // Constantes de escenario
        private const float MAP_SCALE_XZ = 160.0f; // Original = 20
        private const float MAP_SCALE_Y = 10.4f; // Original = 1.3
        private const int FACTOR = 8; // Significa las veces que se agrandó según el MAP_SCALE original.
        // Esto se hace así porque ya hay valores hardcodeados de posiciones que no quiero cambiar.
        // Habría que ver una forma de ubicar meshes en posición relativa en el espacio.

		private Vector3 CENTRO = new Vector3(0, 0, 0);

		// Menu		private Menu menu;

		// Para saber si el juego esta inicializado
		private bool gameLoaded;

		// Tamanio de pantalla
		private Size windowSize;

        // Escenario
        private TgcSimpleTerrain heightmap;
        private TgcSkyBox skyBox;

        private TgcScene casa;
        private TgcMesh rocaOriginal;
        private TgcMesh palmeraOriginal;
        private TgcMesh pastito;
        private TgcMesh arbusto;
        private TgcMesh faraon;
        private TgcMesh arbolSelvatico;
        private TgcMesh hummer;

        private TgcMesh ametralladora;
        private TgcMesh ametralladora2;
        private TgcMesh canoa;
        private TgcMesh helicopter;
        private TgcMesh camionCisterna;
        private TgcMesh tractor;
        private TgcMesh barril;
        private TgcMesh cajaFuturistica;
        private TgcMesh avionMilitar;
        private TgcMesh tanqueFuturista;

        private TgcBox cajita;
        private TgcMesh cajitaMuniciones;

        //objetos que se replican
        private List<TgcMesh> rocas = new List<TgcMesh>();
        private List<TgcMesh> palmeras = new List<TgcMesh>();
        private List<TgcMesh> pastitos = new List<TgcMesh>();
        private List<TgcMesh> cajitas = new List<TgcMesh>();
        private List<TgcMesh> arbolesSelvaticos = new List<TgcMesh>();
        private List<TgcMesh> arbustitos = new List<TgcMesh>();

        //lista de objetos totales
        private List<TgcMesh> meshes = new List<TgcMesh>();

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
		private bool FPSCamera = false;
        private Quadtree quadtree;

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


			initSkyBox();

			initScene();

			var pmin = new Vector3(-16893, -2000, 17112);
			var pmax = new Vector3(18240, 8884, -18876);
			limits = new TgcBoundingAxisAlignBox(pmin, pmax);

			// Iniciar enemigos
			initEnemigos();

			// Iniciar bounding boxes
			initObstaculos();

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

            meshes.Add(arbolSelvatico);
            meshes.Add(hummer);
            meshes.Add(ametralladora);
            meshes.Add(ametralladora2);
            meshes.Add(canoa);
            meshes.Add(helicopter);
            meshes.Add(camionCisterna);
            meshes.Add(tractor);
            meshes.Add(barril);
            meshes.Add(faraon);
            meshes.Add(avionMilitar);
            meshes.Add(tanqueFuturista);

            meshes.AddRange(pastitos);
            meshes.AddRange(rocas);
            meshes.AddRange(palmeras);
            meshes.AddRange(arbolesSelvaticos);
            meshes.AddRange(arbustitos);
            meshes.AddRange(cajitas);


            //quadtree = new Quadtree();
            //quadtree.create(meshes, limits);
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
					jugador.mover(Input, posicionEnTerreno(jugador.Position.X, jugador.Position.Z), ElapsedTime, obstaculos);

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
	            limits.render();
	            if (!FPSCamera)
	            {
	                skyBox.render();

	            }
	            else
	            {
	                DrawText.drawText(Convert.ToString(Camara.Position), 10, 1000, Color.OrangeRed);
	            }

	            Utils.renderFromFrustum(meshes, Frustum);

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
	            hummer.dispose();
	            canoa.dispose();
	            ametralladora2.dispose();
	            camionCisterna.dispose();
	            helicopter.dispose();
	            avionMilitar.dispose();
	            tractor.dispose();
	            barril.dispose();
	            arbusto.dispose();
	            ametralladora.dispose();
	            tanqueFuturista.dispose();

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

	            meshes.Clear();
	            rocas.Clear();
	            pastitos.Clear();
	            arbolesSelvaticos.Clear();
	            palmeras.Clear();
	            cajitas.Clear();
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

        private void initHeightmap(){
            string heightmapDir = MediaDir + "Heightmaps\\heightmap.jpg";
            string textureDir = MediaDir + "Texturas\\map_v2.jpg";

            heightmap = new TgcSimpleTerrain();

            heightmap.loadHeightmap(heightmapDir, MAP_SCALE_XZ, MAP_SCALE_Y, Camara.LookAt);
            heightmap.loadTexture(textureDir);
        }

        private void initScene() {
            // Variables auxiliares.
            int ultimoElemento;

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
            corregirAltura(casa.Meshes);

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
            corregirAltura(palmeras);

            // Creación de pastitos.
            string pastitoDir = MediaDir + "Meshes\\Vegetation\\Pasto\\Pasto-TgcScene.xml";
            pastito = cargarMesh(pastitoDir);
            pastito.AlphaBlendEnable = true;
            pastito.Scale = new Vector3(0.5f, 0.5f, 0.5f);
            pastito.AutoTransformEnable = false;
            Utils.disponerEnRectanguloXZ(pastito, pastitos, 20, 20, 50);
            foreach (var pasto in pastitos)
            {
                var despl = new Vector3(0, 0, 8000);
                pasto.Position += despl;

                pasto.Transform = Matrix.Translation(pasto.Scale) * Matrix.Translation(pasto.Position);
            }
            //pongo los pastitos en aleatorio, pero los saco del circulo celeste del medio
            Utils.aleatorioXZExceptoRadioInicial(pastito, pastitos, 4500);

            corregirAltura(pastitos);

            //arbustitos
            arbusto = cargarMesh(MediaDir + "Meshes\\Vegetation\\Arbusto\\Arbusto-TgcScene.xml");
            arbusto.AlphaBlendEnable = true;
            arbusto.Scale = new Vector3(0.5f, 0.5f, 0.5f);
            pastito.AutoTransformEnable = false;
            Utils.aleatorioXZExceptoRadioInicial(arbusto, arbustitos, 2000);
            corregirAltura(arbustitos);

            // Creación de faraón.
            string faraonDir = MediaDir + "Meshes\\Objetos\\EstatuaFaraon\\EstatuaFaraon-TgcScene.xml";
            faraon = cargarMesh(faraonDir);
            faraon.AutoTransformEnable = false;
            faraon.Scale = new Vector3(FACTOR, FACTOR, FACTOR);
            faraon.Transform = Matrix.Scaling(faraon.Scale) * faraon.Transform;
            faraon.updateBoundingBox();
           
            // Creación de cajitas.
            cajita = TgcBox.fromSize(new Vector3(30 * FACTOR, 30 * FACTOR, 30 * FACTOR), TgcTexture.createTexture(MediaDir + "Texturas\\paja4.jpg"));
            Utils.disponerEnRectanguloXZ(cajita.toMesh("cajitaPaja"), cajitas, 2, 2, 250);
            foreach (var cajita in cajitas)
            {
                cajita.AutoTransformEnable = false;
                cajita.Position += new Vector3(-800 * FACTOR, 50, 1400 * FACTOR);
                cajita.Scale = new Vector3(0.25f, 0.25f, 0.25f);
                //cajita.Transform = Matrix.Scaling(cajita.Scale) * Matrix.Translation(cajita.Position) * cajita.Transform;
            }

            //cajitas de municiones
            var center = new Vector3(-12580, 1790, 9915);
            cajitaMuniciones = cargarMesh(MediaDir + "Meshes\\Armas\\CajaMuniciones\\CajaMuniciones-TgcScene.xml");
            cajitaMuniciones.AutoTransformEnable = false;
            cajitaMuniciones.createBoundingBox();
            cajitaMuniciones.updateBoundingBox();
            Utils.disponerEnCirculoXZ(cajitaMuniciones, cajitas, 8, 400, FastMath.QUARTER_PI, 0, center);
            Utils.aleatorioXZExceptoRadioInicial(cajitaMuniciones, cajitas, 25);

            //cajas futuristicas    
            cajaFuturistica = cargarMesh(MediaDir + "Meshes\\Objetos\\CajaMetalFuturistica2\\CajaMetalFuturistica2-TgcScene.xml");
            var cantCajitas = cajitas.Count;
            Utils.aleatorioXZExceptoRadioInicial(cajaFuturistica, cajitas, 25);
            corregirAltura(cajitas);

            for(int i = cantCajitas; i< cajitas.Count; i++)
            {
                var mesh = cajitas[i];
                mesh.Position += new Vector3(0, 50f, 0);
            }

            //ametralladora
            ametralladora2 = cargarMesh(MediaDir + "Meshes\\Armas\\MetralladoraFija2\\MetralladoraFija2-TgcScene.xml");
            ametralladora2.AutoTransformEnable = false;
            ametralladora2.Position = center;
            ametralladora2.Transform = Matrix.Translation(ametralladora2.Position)* ametralladora2.Transform;
            ametralladora2.createBoundingBox();
            ametralladora2.updateBoundingBox();

            //ametralladora
            ametralladora = cargarMesh(MediaDir + "Meshes\\Armas\\MetralladoraFija\\MetralladoraFija-TgcScene.xml");
            ametralladora.AutoTransformEnable = false;
            ametralladora.Position = new Vector3(1894, this.posicionEnTerreno(1894, 10793), 10793);
            ametralladora.Transform = Matrix.Translation(ametralladora.Position);

            //creacion de arboles selvaticos
            arbolSelvatico = cargarMesh(MediaDir + "Meshes\\Vegetation\\ArbolSelvatico\\ArbolSelvatico-TgcScene.xml");
            arbolSelvatico.Position = new Vector3(1000, 0, 400);
            arbolSelvatico.AutoTransformEnable = false;
            arbolSelvatico.Transform = Matrix.Translation(arbolSelvatico.Position) * arbolSelvatico.Transform;
            arbolSelvatico.createBoundingBox();
            arbolSelvatico.updateBoundingBox();

            //TODO: ajustar posicion segun heightmap (Hecho, aunque funciona mal todavía)
            // Frontera este de árboles
            Utils.disponerEnLineaX(arbolSelvatico, arbolesSelvaticos, 50, 450, new Vector3(-6000, posicionEnTerreno(-6000, 15200), 15200));

            //frontera sur de arboles
            var pos = arbolesSelvaticos.Last().Position;
            Utils.disponerEnLineaZ(arbolSelvatico, arbolesSelvaticos, 70, -450, arbolesSelvaticos.Last().Position);
            
            //frontera oeste
            Utils.disponerEnLineaX(arbolSelvatico, arbolesSelvaticos, 72, -450, arbolesSelvaticos.Last().Position);

            //frontera norte
            Utils.disponerEnLineaZ(arbolSelvatico, arbolesSelvaticos, 68, 450, arbolesSelvaticos.Last().Position);
            foreach (var arbol in arbolesSelvaticos)
            {
                arbol.Scale = new Vector3(3.0f, 3.0f, 3.0f);
                arbol.AutoTransformEnable = false;
                arbol.Transform = Matrix.Scaling(arbol.Scale) * arbol.Transform;
            }

            var rndm = new Random();
            var countArboles = arbolesSelvaticos.Count;
            Utils.aleatorioXZExceptoRadioInicial(arbolSelvatico, arbolesSelvaticos, 40);
            for (int i = countArboles; i <= arbolesSelvaticos.Count - 1; i++)
            {                
                var s= rndm.Next(3, 6);
                var arbol = arbolesSelvaticos[i];

                arbol.AutoTransformEnable = false;
                arbol.Scale = new Vector3(s, s, s);
                arbol.Transform = Matrix.Scaling(arbol.Scale) * arbol.Transform;
            }
            corregirAltura(arbolesSelvaticos);

            // Creación de rocas.
            string rocaDir = MediaDir + "Meshes\\Vegetation\\Roca\\Roca-TgcScene.xml";
            rocaOriginal = cargarMesh(rocaDir);
            
            // Rocas en el agua.
            Utils.disponerEnCirculoXZ(rocaOriginal, rocas, 4, 500 * FACTOR, FastMath.PI_HALF);
            foreach (var roca in rocas)
            {
                roca.AutoTransformEnable = false;
                roca.Scale = new Vector3(3 * FACTOR, 2 * FACTOR, 3 * FACTOR);
                roca.Transform = Matrix.Scaling(roca.Scale) * roca.Transform;
            }

            // Frontera oeste de rocas.
            rocaOriginal.Position = new Vector3(1500, 0, -3000);
            rocaOriginal.Scale = new Vector3(4.0f, 4.0f, 4.0f);
            rocaOriginal.AutoTransformEnable = false;
            rocaOriginal.Transform = Matrix.Scaling(rocaOriginal.Scale) * Matrix.Translation(rocaOriginal.Position) * rocaOriginal.Transform;

            rocaOriginal.createBoundingBox();
            rocaOriginal.updateBoundingBox();

            var count = rocas.Count;
            Utils.disponerEnLineaX(rocaOriginal, rocas, 49, -100, new Vector3(1500, 0, -3000));
            for (int i = count; i <= rocas.Count - 1; i++)
            {
                rocas[i].AutoTransformEnable = false;
                rocas[i].Scale = new Vector3(4.0f, 4.0f, 4.0f);
                rocas[i].Transform = Matrix.Scaling(rocas[i].Scale) * rocas[i].Transform;
            }
            corregirAltura(rocas);

            //barril
            barril = cargarMesh(MediaDir + "Meshes\\Objetos\\BarrilPolvora\\BarrilPolvora-TgcScene.xml");
            barril.Position = new Vector3(-6802, 8, 10985);
            barril.updateBoundingBox();

            // Autitos!
            hummer = cargarMesh(MediaDir + "Meshes\\Vehiculos\\Hummer\\Hummer-TgcScene.xml");
            hummer.Position = new Vector3(1754, this.posicionEnTerreno(1754,9723), 9723);
            hummer.Scale = new Vector3(1.1f, 1.08f, 1.25f);
            hummer.AutoTransformEnable = false;
            hummer.Transform = Matrix.Scaling(hummer.Scale) * Matrix.Translation(hummer.Position) * hummer.Transform;
            hummer.createBoundingBox();
            hummer.updateBoundingBox();

            var anotherHummer = hummer.createMeshInstance(hummer.Name + "1");
            anotherHummer.Position = new Vector3(hummer.Position.X + 350, hummer.Position.Y, hummer.Position.Z - 150);
            anotherHummer.Scale = hummer.Scale;
            anotherHummer.rotateY(FastMath.QUARTER_PI);
            anotherHummer.AutoTransformEnable = false;
            anotherHummer.Transform = Matrix.RotationY(anotherHummer.Rotation.Y)
                                       * Matrix.Scaling(anotherHummer.Scale)
                                       * Matrix.Translation(anotherHummer.Position)
                                       * anotherHummer.Transform;
            anotherHummer.createBoundingBox();
            anotherHummer.updateBoundingBox();

            meshes.Add(anotherHummer);

            //helicoptero
            helicopter = cargarMesh(MediaDir + "Meshes\\Vehiculos\\HelicopteroMilitar\\HelicopteroMilitar-TgcScene.xml");
            helicopter.Position = new Vector3(8308, 0, -4263);
            helicopter.AutoTransformEnable = false;
            helicopter.Scale = new Vector3(4f, 4f, 4f);
            helicopter.Transform = Matrix.Scaling(helicopter.Scale) * Matrix.Translation(helicopter.Position) * helicopter.Transform;
            helicopter.createBoundingBox();
            helicopter.BoundingBox.transform(Matrix.Scaling(0.8f, 2.25f, 3.55f) * Matrix.Translation(helicopter.Position));

            //tanque
            tanqueFuturista = cargarMesh(MediaDir + "Meshes\\Vehiculos\\TanqueFuturistaRuedas\\TanqueFuturistaRuedas-TgcScene.xml");
            tanqueFuturista.Position = new Vector3(11000, posicionEnTerreno(11000,6295), 6295);
            tanqueFuturista.Scale = new Vector3(3f, 3f, 3f);
            tanqueFuturista.updateBoundingBox();

            //agrego otra instancia del tanque, la desplazo, roto y ajusto su posicion en Y
            var anotherTank = tanqueFuturista.createMeshInstance(tanqueFuturista.Name + "1");
            var posTanque2 = tanqueFuturista.Position + new Vector3(650, 0, -450);
            anotherTank.Position = new Vector3(posTanque2.X, posicionEnTerreno(posTanque2.X, posTanque2.Z), posTanque2.Z);
            anotherTank.Scale = tanqueFuturista.Scale;
            anotherTank.rotateY(FastMath.PI_HALF);
            
            anotherTank.AutoTransformEnable = false;
            anotherTank.Transform = Matrix.RotationY(anotherTank.Rotation.Y)
                                       * Matrix.Scaling(anotherTank.Scale)
                                       * Matrix.Translation(anotherTank.Position);
            anotherTank.createBoundingBox();
            //acutalizo el bounding box
            anotherTank.BoundingBox.transform(anotherTank.Transform);            
            meshes.Add(anotherTank);

            //avion militar
            avionMilitar = cargarMesh(MediaDir + "Meshes\\Vehiculos\\AvionMilitar\\AvionMilitar-TgcScene.xml");
            avionMilitar.Position = hummer.Position + new Vector3(1050, 50, 250);
            helicopter.AutoTransformEnable = false;
             //escalo,roto y traslado
            avionMilitar.Scale = new Vector3(2f, 2f, 2f);
            avionMilitar.rotateY(FastMath.PI_HALF);
            avionMilitar.Transform = Matrix.RotationY(avionMilitar.Rotation.Y) * Matrix.Scaling(avionMilitar.Scale) * Matrix.Translation(avionMilitar.Position);
            avionMilitar.createBoundingBox();

            avionMilitar.BoundingBox.transform(Matrix.RotationY(avionMilitar.Rotation.Y)
                                            * Matrix.Scaling(2f, 1.2f, 0.3f)
                                            * Matrix.Translation(avionMilitar.Position));

            //tractor
            tractor = cargarMesh(MediaDir + "Meshes\\Vehiculos\\Tractor\\Tractor-TgcScene.xml");
            tractor.Position = new Vector3(-6802,0, 10385);
            tractor.Scale = new Vector3(1.5f, 1f, 1.25f);
            tractor.AutoTransformEnable = false;
            tractor.Transform = Matrix.Translation(tractor.Position) * tractor.Transform;
            tractor.updateBoundingBox();

            //canoa
            canoa = cargarMesh(MediaDir + "Meshes\\Vehiculos\\Canoa\\Canoa-TgcScene.xml");
            canoa.Position = new Vector3(3423, 10, -3847);
            canoa.updateBoundingBox();

            //camionCisterna
            camionCisterna = cargarMesh(MediaDir + "Meshes\\Vehiculos\\CamionCisterna\\CamionCisterna-TgcScene.xml");
            helicopter.AutoTransformEnable = false;
            camionCisterna.Position = new Vector3(227, 0, 10719);
            camionCisterna.Scale = new Vector3(2f, 2f, 2f);
            camionCisterna.Transform = Matrix.Scaling(camionCisterna.Scale) * Matrix.Translation(camionCisterna.Position) * camionCisterna.Transform;
            camionCisterna.updateBoundingBox();
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
            //aniadirObstaculoAABB(casa.Meshes);            

            //bounging cilinders de las palmeras
            foreach (var palmera in palmeras)
            {
                var despl = new Vector3(0, 100, 0);
                var cilindro = new TgcBoundingCylinderFixedY(
                    palmera.Position + despl, 20, 100);

                collisionManager.agregarCylinder(cilindro);
            }
            
            //aniadirObstaculoAABB(cajitas);
            CollisionManager.Instance.setPlayer(jugador);

            //bounding cyilinder del arbol
            var adjustPos =new Vector3(0, 0, 44);
            //var cylinder = new TgcBoundingCylinderFixedY(arbolSelvatico.BoundingBox.calculateBoxCenter(), 60, 200);
            var cylinder = new TgcBoundingCylinderFixedY(arbolSelvatico.BoundingBox.calculateBoxCenter(), 60, 200);
            CollisionManager.Instance.agregarCylinder(cylinder);
            
           //bounding cilinders de los arbolesSelvaticos
           foreach (var arbol in arbolesSelvaticos)
           {
                var despl = new Vector3(0,400, 0);

                arbol.Transform = Matrix.Scaling(arbol.Scale) * Matrix.Translation(arbol.Position);
                arbol.createBoundingBox();
                arbol.updateBoundingBox();
                
                var radio = 60 * arbol.Scale.X;
                var height = 200 * arbol.Scale.Y;
                var cilindro = new TgcBoundingCylinderFixedY(arbol.BoundingBox.calculateBoxCenter(), radio, height);

                //collisionManager.agregarAABB(arbol.BoundingBox);
                collisionManager.agregarCylinder(cilindro);
           }
            //bounding box de las rocas
            foreach (var roca in rocas)
            {
                var center = roca.BoundingBox.calculateBoxCenter();
                var radio = roca.BoundingBox.calculateAxisRadius();

                var cilindro = new TgcBoundingCylinderFixedY(center, radio.X * 0.95f, radio.Y);

                collisionManager.agregarCylinder(cilindro);
            }

            //bounding cylinder del barril
            var barrilCylinder = new TgcBoundingCylinderFixedY(barril.BoundingBox.calculateBoxCenter(), barril.BoundingBox.calculateBoxRadius() - 18, 24);
            collisionManager.agregarCylinder(barrilCylinder);

            //cilindro del faraon del mesio
            var faraonCylinder = new TgcBoundingCylinderFixedY(faraon.BoundingBox.calculateBoxCenter(), faraon.BoundingBox.calculateBoxRadius() * 0.15f, 1500);
            collisionManager.agregarCylinder(faraonCylinder);

            collisionManager.agregarAABB(canoa.BoundingBox);
            collisionManager.agregarAABB(helicopter.BoundingBox);
            collisionManager.agregarAABB(camionCisterna.BoundingBox);
            collisionManager.agregarAABB(tractor.BoundingBox);

            collisionManager.agregarAABB(tanqueFuturista.BoundingBox);
            foreach (var mesh in tanqueFuturista.MeshInstances)
            {               
                collisionManager.agregarAABB(mesh.BoundingBox);
            }

            //bounding box de las hummer
            collisionManager.agregarAABB(hummer.BoundingBox);
            foreach (var mesh in hummer.MeshInstances)
            {
                var obb = new TgcBoundingOrientedBox();

                obb.Center = mesh.BoundingBox.calculateBoxCenter();
                obb.Extents = mesh.BoundingBox.calculateAxisRadius();

                obb.setRotation(new Vector3(0, mesh.Rotation.Y, 0));
                collisionManager.agregarAABB(mesh.BoundingBox);
                collisionManager.agregarOBB(obb);
            }
            
            //bounding box del avion militar
            collisionManager.agregarAABB(avionMilitar.BoundingBox);
            //este seria el bb de las alas
            var otroBoundingBox = helicopter.BoundingBox.clone();
            otroBoundingBox.transform(Matrix.RotationY(avionMilitar.Rotation.Y)
                                             * Matrix.Scaling(0.3f, 1f, 2.3f)
                                             * Matrix.Translation(avionMilitar.Position - new Vector3(52, 0, 0)));
            collisionManager.agregarAABB(otroBoundingBox);

            //bounding box de las cajas
            foreach (var caja in cajitas)
            {
                caja.Transform = Matrix.Scaling(caja.Scale) * Matrix.Translation(caja.Position);
                caja.createBoundingBox();
                caja.updateBoundingBox();
                collisionManager.agregarAABB(caja.BoundingBox);
            }
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
            texto.Text += "\nPosition\n" + jugador.Position;

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
