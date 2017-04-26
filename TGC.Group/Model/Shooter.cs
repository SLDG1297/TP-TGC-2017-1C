using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Terrain;
using TGC.Core.SceneLoader;
using TGC.Group.Model.Cameras;
using System.Collections.Generic;
using TGC.Core.Utils;
using TGC.Core.Geometry;
using TGC.Core.Textures;
using TGC.Core.Collision;
using TGC.Core.BoundingVolumes;
using TGC.Group.Model.Entities;
using System.Drawing;

namespace TGC.Group.Model
{
    public  class Shooter : TgcExample
    {
        //CONSTANTES
        private const float MAP_SCALE_XZ = 20.0f;
        private const float MAP_SCALE_Y = 1.3f;
        private Vector3 CENTRO = new Vector3(0, 0, 0);
        private Vector3 PLAYER_INIT_POS = new Vector3(500, 0, 500);

		// Rotacion de la camara segun el puntero del mouse
		private float leftrightRot = 0f;

        //VARIABLES DE INSTANCIA
        private TgcSimpleTerrain terreno;
		private TgcSkyBox skyBox;
        private TgcScene scene;
        private TgcScene casa;

        private ThirdPersonCamera camaraInterna;

#region OBJETOS QUE SE REPLICAN
        private TgcMesh rocaOriginal;
        private List<TgcMesh> rocas = new List<TgcMesh>();

        private TgcMesh palmeraOriginal;
        private List<TgcMesh> palmeras = new List<TgcMesh>();

        private TgcBox cajita;
        private List<TgcMesh> cajitas = new List<TgcMesh>();

        private TgcMesh pastito;
        private List<TgcMesh> pastitos = new List<TgcMesh>();
 #endregion

        private Player jugador;

        //por ahora es para probar con uno solo, mas adelante podemos tener mas de uno
        //private List<Enemy> enemigo = new List<Enemy>();
        private Enemy enemigo;

		private List<TgcBoundingAxisAlignBox> obstaculos = new List<TgcBoundingAxisAlignBox>(); // Colisiones

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
        }

        public override void Init()
        {
            jugador = new Player(MediaDir,"CS_Gign", PLAYER_INIT_POS, Arma.AK47(MediaDir));
            enemigo = new Enemy(MediaDir, "CS_Arctic", new Vector3(500, 0, 400), Arma.AK47(MediaDir));         
            //Camara = new FirstPersonCamera(new Vector3(0, 1500, 0), Input);

            initSkyBox();
            initTerrain();
            initScene();

			//Configurar camara en Tercera Persona y la asigno al TGC.
            camaraInterna = new ThirdPersonCamera(jugador, new Vector3(-40,0,-50), 50, 150, Input);
            //camaraInterna = new ThirdPersonCamera(jugador, 50, 150, Input);
            Camara = camaraInterna;

			initObstaculos();
        }

        public override void Update()
        {
            PreUpdate();
            
			jugador.mover(Input, ElapsedTime, obstaculos);

			leftrightRot -= -Input.XposRelative * 0.05f;

			camaraInterna.RotationY = leftrightRot;

            //Hacer que la camara siga al personaje en su nueva posicion
            camaraInterna.Target = jugador.Position;
        }

        public override void Render()
        {
            //Inicio el render de la escena, para ejemplos simples.
            // Cuando tenemos postprocesado o shaders es mejor realizar las operaciones según nuestra conveniencia.
            PreRender();

			skyBox.render();

            //Renderizar instancias de las rocas y palmeras del medio
            Utils.renderMeshes(rocas);
            Utils.renderMeshes(palmeras);
            Utils.renderMeshes(pastitos);
            Utils.renderMeshes(cajitas);

            terreno.render();
            scene.renderAll();
            casa.renderAll();
            jugador.render(ElapsedTime);
            enemigo.render(ElapsedTime);


            DrawText.drawText("HEALTH: " + jugador.Health + "; BALAS: " + jugador.Arma.Balas + "; RECARGAS: " + jugador.Arma.Recargas, 50, 1000, Color.OrangeRed);


            renderAABB();

            //Finaliza el render y presenta en pantalla, al igual que el preRender se debe para casos puntuales es mejor utilizar a mano las operaciones de EndScene y PresentScene
            PostRender();
        }

        public override void Dispose()
        {
            skyBox.dispose();
            terreno.dispose();

            rocaOriginal.dispose();
            palmeraOriginal.dispose();
            pastito.dispose();
            cajita.dispose();

            scene.disposeAll();
            casa.disposeAll();

            jugador.dispose();
            enemigo.dispose();

			foreach (var obstaculo in obstaculos)
            {
                obstaculo.dispose();
            }
        }

#region METODOS AUXILIARES
        private void initTerrain(){

            string heightmapDir = MediaDir + "Heightmaps\\heightmap_v2.jpg";
            string terrainTextureDir = MediaDir + "Texturas\\map_v2.jpg";

            terreno = new TgcSimpleTerrain();
            terreno.loadHeightmap(heightmapDir, MAP_SCALE_XZ, MAP_SCALE_Y, Camara.LookAt);
            terreno.loadTexture(terrainTextureDir);
        }

        private void initScene(){
            var loader = new TgcSceneLoader();
            palmeraOriginal = loader.loadSceneFromFile(MediaDir + "Meshes\\Vegetation\\Palmera\\Palmera-TgcScene.xml").Meshes[0];

            scene = loader.loadSceneFromFile(MediaDir + "Scenes\\Arboles00\\EscenaConArboles-TgcScene.xml");
            casa = loader.loadSceneFromFile(MediaDir + "Meshes\\Edificios\\Casa\\Casa-TgcScene.xml");
            pastito = scene.getMeshByName("Arbusto");

             //Dispongo las rocas en linea circular y luego la escalo
            rocaOriginal = scene.getMeshByName("Roca");
            Utils.disponerEnCirculoXZ(rocaOriginal, rocas, 4, 500, FastMath.PI_HALF);

            foreach (var roca in rocas){
                roca.Transform = Matrix.Scaling(6, 4, 6) * roca.Transform;
            }

            //Dispongo las palmeras en forma circular
            Utils.disponerEnCirculoXZ(palmeraOriginal, palmeras, 8, 820, FastMath.QUARTER_PI);

            //ubico la casa, trasladandola y luego rotandola
            foreach (var mesh in casa.Meshes)
            {
                mesh.AutoTransformEnable = false;
                mesh.Transform = Matrix.Scaling(1.5f,2f,1.75f) *Matrix.RotationY(FastMath.PI_HALF + FastMath.PI) * Matrix.Translation(-800, 0, 1200);
            }
            
            //creo cajitas de paja y las ubico
            cajita = TgcBox.fromSize(new Vector3(30,30,30), TgcTexture.createTexture(MediaDir + "Texturas\\paja4.jpg"));
            Utils.disponerEnRectanguloXZ(cajita.toMesh("cajita"), cajitas, 2, 2, 50);
            foreach (var mesh in cajitas)
            {
                mesh.AutoTransformEnable = false;
                mesh.Transform = Matrix.Translation(-800, 20, 1400) * mesh.Transform;
            }            

            // SOLUCIÓN NUEVA A LO QUE APARECÍA EN EL CENTRO:
            scene.Meshes.RemoveAll(mesh => mesh.Position == CENTRO);
        }

        private void initSkyBox(){

            //Crear SkyBox
            skyBox = new TgcSkyBox();
            skyBox.Center = new Vector3(0, 500, 0);
            skyBox.Size = new Vector3(8000, 8000, 8000);

			var texturesPath = MediaDir + "Texturas\\Quake\\SkyBoxWhale\\Whale";

			skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Up, texturesPath + "up.jpg");
			skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Down, texturesPath + "dn.jpg");
			skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Left, texturesPath + "lf.jpg");
			skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Right, texturesPath + "rt.jpg");
			skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Front, texturesPath + "bk.jpg");
			skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Back, texturesPath + "ft.jpg");

            skyBox.Init();
        }

		private void initObstaculos() {
			foreach (var mesh in scene.Meshes) {
				obstaculos.Add(mesh.BoundingBox);
			}
			foreach (var mesh in casa.Meshes) {
				obstaculos.Add(mesh.BoundingBox);
			}
		}

		// Renderizar bounding box
		private void renderAABB() {
			jugador.Esqueleto.BoundingBox.render();
			enemigo.Esqueleto.BoundingBox.render();
			foreach (var obstaculo in obstaculos) {
				obstaculo.render();
			}
		}
#endregion
    }
}
