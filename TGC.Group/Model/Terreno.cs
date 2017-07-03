using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.Direct3D;
using TGC.Core.SceneLoader;
using TGC.Core.Shaders;
using TGC.Core.Terrain;
using TGC.Core.Utils;
using TGC.Group.Model.Environment;

namespace TGC.Group.Model
{
    public class Terreno
    {
        // Constantes de escenario
        private const float MAP_SCALE_XZ = 160.0f; // Original = 20
        private const float MAP_SCALE_Y = 10.4f; // Original = 1.3
        //private TgcSimpleTerrain heightmap;
        public Texture terrainTexture;

        public Vector3 center;
        public float ftex = 1f; // factor para la textura
        public int[,] heightmapData;
        public float ki = 1;
        public float kj = 1;
        public float radio_1 = 0;
        public float radio_2 = 0;
        public bool torus = false;
        public int totalVertices;
        private VertexBuffer vbTerrain;

        public Terreno(string MediaDir, Vector3 center)
        {
            string heightmapDir = MediaDir + "Heightmaps\\heightmap.jpg";
            string textureDir = MediaDir + "Texturas\\map_v2.jpg";

            //heightmap = new TgcSimpleTerrain();
            //heightmap.loadHeightmap(heightmapDir, MAP_SCALE_XZ, MAP_SCALE_Y, center);

            this.center = center;
            loadHeightMap(heightmapDir);
            loadTexture(textureDir);
        }

        public void loadHeightMap(string heightmapDir)
        {

            //Dispose de VertexBuffer anterior, si habia
            if (vbTerrain != null && !vbTerrain.Disposed)
            {
                vbTerrain.Dispose();
            }

            //cargar heightmap
            heightmapData = loadHeightMap(D3DDevice.Instance.Device, heightmapDir);
            float width = heightmapData.GetLength(0);
            float length = heightmapData.GetLength(1);

            //Crear vertexBuffer
            totalVertices = 2 * 3 * (heightmapData.GetLength(0) + 1) * (heightmapData.GetLength(1) + 1);
            totalVertices *= (int)ki * (int)kj;
            vbTerrain = new VertexBuffer(typeof(CustomVertex.PositionTextured), totalVertices,
                D3DDevice.Instance.Device,
                Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionTextured.Format, Pool.Default);

            //Cargar vertices
            var dataIdx = 0;
            var data = new CustomVertex.PositionTextured[totalVertices];

            center.X = center.X * MAP_SCALE_XZ - width / 2 * MAP_SCALE_XZ;
            center.Y = center.Y * MAP_SCALE_Y;
            center.Z = center.Z * MAP_SCALE_XZ - length / 2 * MAP_SCALE_XZ;

            if (torus)
            {
                var di = width * ki;
                var dj = length * kj;

                for (var i = 0; i < width * ki; i++)
                {
                    for (var j = 0; j < length * kj; j++)
                    {
                        var ri = i % (int)width;
                        var rj = j % (int)length;
                        var ri1 = (i + 1) % (int)width;
                        var rj1 = (j + 1) % (int)length;

                        Vector3 v1, v2, v3, v4;
                        {
                            var r = radio_2 + heightmapData[ri, rj] * MAP_SCALE_Y;
                            var s = 2f * (float)Math.PI * j / dj;
                            var t = -(float)Math.PI * i / di;
                            var x = (float)Math.Cos(s) * (radio_1 + r * (float)Math.Cos(t));
                            var z = (float)Math.Sin(s) * (radio_1 + r * (float)Math.Cos(t));
                            var y = r * (float)Math.Sin(t);
                            v1 = new Vector3(x, y, z);
                        }
                        {
                            var r = radio_2 + heightmapData[ri, rj1] * MAP_SCALE_Y;
                            var s = 2f * (float)Math.PI * (j + 1) / dj;
                            var t = -(float)Math.PI * i / di;
                            var x = (float)Math.Cos(s) * (radio_1 + r * (float)Math.Cos(t));
                            var z = (float)Math.Sin(s) * (radio_1 + r * (float)Math.Cos(t));
                            var y = r * (float)Math.Sin(t);
                            v2 = new Vector3(x, y, z);
                        }
                        {
                            var r = radio_2 + heightmapData[ri1, rj] * MAP_SCALE_Y;
                            var s = 2f * (float)Math.PI * j / dj;
                            var t = -(float)Math.PI * (i + 1) / di;
                            var x = (float)Math.Cos(s) * (radio_1 + r * (float)Math.Cos(t));
                            var z = (float)Math.Sin(s) * (radio_1 + r * (float)Math.Cos(t));
                            var y = r * (float)Math.Sin(t);
                            v3 = new Vector3(x, y, z);
                        }
                        {
                            var r = radio_2 + heightmapData[ri1, rj1] * MAP_SCALE_Y;
                            var s = 2f * (float)Math.PI * (j + 1) / dj;
                            var t = -(float)Math.PI * (i + 1) / di;
                            var x = (float)Math.Cos(s) * (radio_1 + r * (float)Math.Cos(t));
                            var z = (float)Math.Sin(s) * (radio_1 + r * (float)Math.Cos(t));
                            var y = r * (float)Math.Sin(t);
                            v4 = new Vector3(x, y, z);
                        }

                        //Coordendas de textura
                        var t1 = new Vector2(ftex * i / width, ftex * j / length);
                        var t2 = new Vector2(ftex * i / width, ftex * (j + 1) / length);
                        var t3 = new Vector2(ftex * (i + 1) / width, ftex * j / length);
                        var t4 = new Vector2(ftex * (i + 1) / width, ftex * (j + 1) / length);

                        //Cargar triangulo 1
                        data[dataIdx] = new CustomVertex.PositionTextured(v1, t1.X, t1.Y);
                        data[dataIdx + 1] = new CustomVertex.PositionTextured(v2, t2.X, t2.Y);
                        data[dataIdx + 2] = new CustomVertex.PositionTextured(v4, t4.X, t4.Y);

                        //Cargar triangulo 2
                        data[dataIdx + 3] = new CustomVertex.PositionTextured(v1, t1.X, t1.Y);
                        data[dataIdx + 4] = new CustomVertex.PositionTextured(v4, t4.X, t4.Y);
                        data[dataIdx + 5] = new CustomVertex.PositionTextured(v3, t3.X, t3.Y);

                        dataIdx += 6;
                    }
                }
            }
            else
            {
                for (var i = 0; i < width - 1; i++)
                {
                    for (var j = 0; j < length - 1; j++)
                    {
                        //Vertices
                        var v1 = new Vector3(center.X + i * MAP_SCALE_XZ, center.Y + heightmapData[i, j] * MAP_SCALE_Y,
                            center.Z + j * MAP_SCALE_XZ);
                        var v2 = new Vector3(center.X + i * MAP_SCALE_XZ, center.Y + heightmapData[i, j + 1] * MAP_SCALE_Y,
                            center.Z + (j + 1) * MAP_SCALE_XZ);
                        var v3 = new Vector3(center.X + (i + 1) * MAP_SCALE_XZ, center.Y + heightmapData[i + 1, j] * MAP_SCALE_Y,
                            center.Z + j * MAP_SCALE_XZ);
                        var v4 = new Vector3(center.X + (i + 1) * MAP_SCALE_XZ, center.Y + heightmapData[i + 1, j + 1] * MAP_SCALE_Y,
                            center.Z + (j + 1) * MAP_SCALE_XZ);

                        //Coordendas de textura
                        var t1 = new Vector2(ftex * i / width, ftex * j / length);
                        var t2 = new Vector2(ftex * i / width, ftex * (j + 1) / length);
                        var t3 = new Vector2(ftex * (i + 1) / width, ftex * j / length);
                        var t4 = new Vector2(ftex * (i + 1) / width, ftex * (j + 1) / length);

                        //Cargar triangulo 1
                        data[dataIdx] = new CustomVertex.PositionTextured(v1, t1.X, t1.Y);
                        data[dataIdx + 1] = new CustomVertex.PositionTextured(v2, t2.X, t2.Y);
                        data[dataIdx + 2] = new CustomVertex.PositionTextured(v4, t4.X, t4.Y);

                        //Cargar triangulo 2
                        data[dataIdx + 3] = new CustomVertex.PositionTextured(v1, t1.X, t1.Y);
                        data[dataIdx + 4] = new CustomVertex.PositionTextured(v4, t4.X, t4.Y);
                        data[dataIdx + 5] = new CustomVertex.PositionTextured(v3, t3.X, t3.Y);

                        dataIdx += 6;
                    }
                }
            }

            vbTerrain.SetData(data, 0, LockFlags.None);
        }
        
        /// <summary>
        ///     Carga la textura del terreno
        /// </summary>
        public void loadTexture(string path)
        {
            //Dispose textura anterior, si habia
            if (terrainTexture != null && !terrainTexture.Disposed)
            {
                terrainTexture.Dispose();
            }

            //Rotar e invertir textura
            var b = (Bitmap)Image.FromFile(path);
            b.RotateFlip(RotateFlipType.Rotate90FlipX);
            terrainTexture = Texture.FromBitmap(D3DDevice.Instance.Device, b, Usage.None, Pool.Managed);
        }

        private int[,] loadHeightMap(Device d3dDevice, string path)
        {
            var bitmap = (Bitmap)Image.FromFile(path);
            var width = bitmap.Size.Width;
            var height = bitmap.Size.Height;
            var heightmap = new int[width, height];
            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    //(j, i) invertido para primero barrer filas y despues columnas
                    var pixel = bitmap.GetPixel(j, i);
                    var intensity = pixel.R * 0.299f + pixel.G * 0.587f + pixel.B * 0.114f;
                    heightmap[i, j] = (int)intensity;
                }
            }

            bitmap.Dispose();
            return heightmap;
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

            var H0 = heightmapData[pi, pj] * MAP_SCALE_Y;
            var H1 = heightmapData[pi1, pj] * MAP_SCALE_Y;
            var H2 = heightmapData[pi, pj1] * MAP_SCALE_Y;
            var H3 = heightmapData[pi1, pj1] * MAP_SCALE_Y;
            var H = (H0 * (1 - fracc_i) + H1 * fracc_i) * (1 - fracc_j) +
                    (H2 * (1 - fracc_i) + H3 * fracc_i) * fracc_j;
            return H;
        }

        public float posicionEnTerreno(Vector3 posicion)
        {
            return posicionEnTerreno(posicion.X, posicion.Z);
        }

        public bool estaEnElPiso(Vector3 posicion)
        {
            return posicion.Y < 0 || FastMath.Abs(posicion.Y - this.posicionEnTerreno(posicion)) < 10.0f;
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

        //public void render()
        //{
        //    heightmap.render();
        //}

        public void render()
        {
            D3DDevice.Instance.Device.Transform.World = Matrix.Identity;

            //Render terrain
            D3DDevice.Instance.Device.SetTexture(0, terrainTexture);
            D3DDevice.Instance.Device.SetTexture(1, null);
            D3DDevice.Instance.Device.Material = D3DDevice.DEFAULT_MATERIAL;

            D3DDevice.Instance.Device.VertexFormat = CustomVertex.PositionTextured.Format;
            D3DDevice.Instance.Device.SetStreamSource(0, vbTerrain, 0);
            D3DDevice.Instance.Device.DrawPrimitives(PrimitiveType.TriangleList, 0, totalVertices / 3);
        }

        public void executeRender(Effect effect)
        {
            TgcShaders.Instance.setShaderMatrixIdentity(effect);

            //Render terrain
            effect.SetValue("texDiffuseMap", terrainTexture);

            D3DDevice.Instance.Device.VertexFormat = CustomVertex.PositionTextured.Format;
            D3DDevice.Instance.Device.SetStreamSource(0, vbTerrain, 0);

            var numPasses = effect.Begin(0);
            for (var n = 0; n < numPasses; n++)
            {
                effect.BeginPass(n);
                D3DDevice.Instance.Device.DrawPrimitives(PrimitiveType.TriangleList, 0, totalVertices / 3);
                effect.EndPass();
            }
            effect.End();
        }

        //public void dispose()
        //{
        //    heightmap.dispose();
        //}
        public void dispose()
        {
            if (vbTerrain != null)
            {
                vbTerrain.Dispose();
            }
            if (terrainTexture != null)
            {
                terrainTexture.Dispose();
            }
        }
    }
}
