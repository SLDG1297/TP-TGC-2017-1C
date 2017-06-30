using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.SceneLoader;
using TGC.Core.Terrain;
using TGC.Group.Model.Environment;

namespace TGC.Group.Model
{
    public class Terreno
    {
        // Constantes de escenario
        private const float MAP_SCALE_XZ = 160.0f; // Original = 20
        private const float MAP_SCALE_Y = 10.4f; // Original = 1.3
        private TgcSimpleTerrain heightmap;

        public Terreno(string MediaDir, Vector3 center)
        {
            string heightmapDir = MediaDir + "Heightmaps\\heightmap.jpg";
            string textureDir = MediaDir + "Texturas\\map_v2.jpg";

            heightmap = new TgcSimpleTerrain();

            heightmap.loadHeightmap(heightmapDir, MAP_SCALE_XZ, MAP_SCALE_Y, center);
            heightmap.loadTexture(textureDir);
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

        public void corregirAltura(List<TgcMesh> meshes)
        {
            foreach (var mesh in meshes)
            {
                float posicionY = posicionEnTerreno(mesh.Position.X, mesh.Position.Z);
                mesh.Position = new Vector3(mesh.Position.X, posicionY, mesh.Position.Z);
                mesh.Transform = Matrix.Translation(0, posicionY, 0) * mesh.Transform;
            }
        }

        public void corregirAltura(List<Barril> barriles)
        {
            foreach (var barril in barriles)
            {
                float posicionY = posicionEnTerreno(barril.Position.X, barril.Position.Z);

                barril.setPosition(new Vector3(barril.Position.X, posicionY + 8, barril.Position.Z));
                //barril.Mesh.Transform = Matrix.Translation(barril.Mesh.Position);
            }
        }

        public void render()
        {
            heightmap.render();
        }

        public void dispose()
        {
            heightmap.dispose();
        }
    }
}
