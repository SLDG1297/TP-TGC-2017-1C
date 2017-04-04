using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Terrain;
using TGC.Group.Model.Cameras;

namespace TGC.Group.Model
{
    public  class Shooter : TgcExample
    {
        //CONSTANTES
        private const float MAP_SCALE_XZ = 15;
        private const float MAP_SCALE_Y = 3;

        //VARIABLES DE INSTANCIA
        private TgcSimpleTerrain terreno;
		private TgcSkyBox skyBox;

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
            var d3dDevice = D3DDevice.Instance.Device;

            Camara = new FirstPersonCamera(new Vector3(0, 1500, 0), Input);
            initSkyBox();
            initTerrain();
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

            terreno.render();

            //Finaliza el render y presenta en pantalla, al igual que el preRender se debe para casos puntuales es mejor utilizar a mano las operaciones de EndScene y PresentScene
            PostRender();
        }

        public override void Dispose()
        {
            terreno.dispose();
        }

#region METODOS AUXILIARES
        private void initTerrain()
        {
            string heightmapDir = MediaDir + "Heightmaps\\heightmap_v2.jpg";
            string terrainTextureDir = MediaDir + "Texturas\\map_v2.jpg";

            terreno = new TgcSimpleTerrain();
            terreno.loadHeightmap(heightmapDir, MAP_SCALE_XZ, MAP_SCALE_Y, Camara.LookAt);
            terreno.loadTexture(terrainTextureDir);
        }

        private void initSkyBox()
        {
            //Crear SkyBox
            skyBox = new TgcSkyBox();
            skyBox.Center = new Vector3(0, 500, 0);
            skyBox.Size = new Vector3(8000, 8000, 8000);
            var texturesPath = MediaDir + "Texturas\\Quake\\SkyBox LostAtSeaDay\\";

            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Up, texturesPath + "lostatseaday_up.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Down, texturesPath + "lostatseaday_dn.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Left, texturesPath + "lostatseaday_lf.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Right, texturesPath + "lostatseaday_rt.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Front, texturesPath + "lostatseaday_bk.jpg");
            skyBox.setFaceTexture(TgcSkyBox.SkyFaces.Back, texturesPath + "lostatseaday_ft.jpg");

            skyBox.Init();
        }


#endregion



    }
}
