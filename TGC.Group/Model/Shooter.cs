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

namespace TGC.Group.Model
{
    public  class Shooter : TgcExample
    {
        //CONSTANTES
        private const float MAP_SCALE_XZ = 20.0f;
        private const float MAP_SCALE_Y = 1.3f;
        private Vector3 CENTRO = new Vector3(0, 0, 0);

        //VARIABLES DE INSTANCIA
        private TgcSimpleTerrain terreno;
		private TgcSkyBox skyBox;
        private TgcScene scene;
        
        private TgcMesh rocaOriginal;
        private List<TgcMesh> rocas = new List<TgcMesh>();

        private TgcMesh palmeraOriginal;
        private List<TgcMesh> palmeras = new List<TgcMesh>();


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
            //Device de DirectX para crear primitivas.
            //var d3dDevice = D3DDevice.Instance.Device;

            Camara = new FirstPersonCamera(new Vector3(0, 1500, 0), Input);
            initSkyBox();
            initTerrain();
            initScene();

            //Dispongo las rocas en linea circular y luego la escalo
            rocaOriginal = scene.getMeshByName("Roca");
            Utils.disponerEnCirculoXZ(rocaOriginal, rocas, 4, 500, FastMath.PI_HALF);

            foreach (var roca in rocas){
                roca.Transform = Matrix.Scaling(6, 4, 6) * roca.Transform;
            }

            Utils.disponerEnCirculoXZ(palmeraOriginal, palmeras, 8, 825, FastMath.QUARTER_PI);

            scene.getMeshByName("Canoa1").Transform = Matrix.Translation(0, 4, 0);

            // SOLUCIÓN NUEVA A LO QUE APARECÍA EN EL CENTRO:
            scene.Meshes.RemoveAll(mesh => mesh.Position == CENTRO);
            
            // SOLUCIÓN ANTERIOR A LO QUE APARECÍA EN EL CENTRO:
            /*
            scene.Meshes.Remove(scene.getMeshByName("ArbolSelvatico"));
            scene.Meshes.Remove(scene.getMeshByName("ArbolSelvatico2"));
            scene.Meshes.Remove(scene.getMeshByName("Hummer"));
            scene.Meshes.Remove(scene.getMeshByName("Canoa"));
            scene.Meshes.Remove(scene.getMeshByName("MetralladoraFija"));
            scene.Meshes.Remove(scene.getMeshByName("MetralladoraFija2"));
            scene.Meshes.Remove(scene.getMeshByName("BarrilPolvora"));
            scene.Meshes.Remove(scene.getMeshByName("CajaMuniciones"));
            scene.Meshes.Remove(scene.getMeshByName("Pasto"));
            scene.Meshes.Remove(scene.getMeshByName("Roca"));
            scene.Meshes.Remove(scene.getMeshByName("CamionCarga"));
            scene.Meshes.Remove(scene.getMeshByName("Arbusto"));
            scene.Meshes.Remove(scene.getMeshByName("Barrera"));
            scene.Meshes.Remove(scene.getMeshByName("Carretilla"));
            */

        }

        public override void Update()
        {
            PreUpdate();
        }

        public override void Render()
        {
            //Inicio el render de la escena, para ejemplos simples.
            // Cuando tenemos postprocesado o shaders es mejor realizar las operaciones según nuestra conveniencia.
            PreRender();

			skyBox.render();
           
                     
            //Renderizar instancias de las rocas y palmeras del medio
            foreach (var mesh in rocas) mesh.render();
            foreach (var mesh in palmeras) mesh.render();
            terreno.render();
            scene.renderAll();

            //Finaliza el render y presenta en pantalla, al igual que el preRender se debe para casos puntuales es mejor utilizar a mano las operaciones de EndScene y PresentScene
            PostRender();
        }

        public override void Dispose()
        {
            skyBox.dispose();

            terreno.dispose();

            rocaOriginal.dispose();
            palmeraOriginal.dispose();
            scene.disposeAll(); 
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
#endregion
    }
}
