using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.BoundingVolumes;
using TGC.Core.Collision;
using TGC.Core.Geometry;
using TGC.Core.SceneLoader;
using TGC.Core.Terrain;
using TGC.Core.Utils;

namespace TGC.Group.Model
{
    class Utils
    {
        /// <summary>
        ///     Dispone un mesh en forma de circulo n veces dado un radio y el angulo. El angulo de fase es 0
        /// </summary>
        /// <param name="orginalMesh">Ruta donde esta la carpeta con los assets</param>
        /// <param name="lista">Lista del elemento a replicar. Debe estar instanciada</param>
        public static void disponerEnCirculoXZ(TgcMesh originalMesh, List<TgcMesh> lista, int veces, float radio, float angulo)
        {
            disponerEnCirculoXZ(originalMesh, lista, veces, radio, angulo, 0);
        }


        public static void disponerEnCirculoXZ(TgcMesh originalMesh, List<TgcMesh> lista, int veces, float radio,float angulo, float anguloFase, Vector3 center)
        {
            for (int i = 0; i < veces; i++)
            {
                //Crear instancia de modelo
                var instance = originalMesh.createMeshInstance(originalMesh.Name + i);
                instance.AutoTransformEnable = false;

                var position = new Vector3(center.X + radio * FastMath.Cos((i * angulo) + anguloFase),
                                           center.Y ,
                                           center.Z + radio * FastMath.Sin((i * angulo) + anguloFase));
                instance.Position = position;

                instance.Transform = Matrix.Translation(instance.Position)
                                    * instance.Transform;

                lista.Add(instance);
            }
        }

        /// <summary>
        ///     Dispone un mesh en forma de circulo n veces dado un radio, el angulo de fase y un angulo de desplazamiento.
        /// </summary>
        /// <param name="orginalMesh">Ruta donde esta la carpeta con los assets</param>
        /// <param name="lista">Lista del elemento a replicar. Debe estar instanciada</param>
        /// <param name="anguloFase">Angulo sobre el cual se comienza la disposicion</param>
        public static void disponerEnCirculoXZ(TgcMesh originalMesh, List<TgcMesh> lista, int veces, float radio, float angulo, float anguloFase)
        {
            for (int i = 0; i < veces; i++)
            {
                //Crear instancia de modelo
                var instance = originalMesh.createMeshInstance(originalMesh.Name + i);
                instance.AutoTransformEnable = false;

                var position = new Vector3(radio * FastMath.Cos((i * angulo) + anguloFase),
                                                        0,
                                                        radio * FastMath.Sin((i * angulo) + anguloFase));
                instance.Position = position;

                instance.Transform = Matrix.Translation(instance.Position)
                                    * instance.Transform;

                lista.Add(instance);
            }
        }
        
        public static void disponerEnRectanguloXZ(TgcMesh originalMesh, List<TgcMesh> meshes, int rows, int cols, float offset)
        {
            //Crear varias instancias del modelo original, pero sin volver a cargar el modelo entero cada vez
            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < cols; j++)
                {
                    //Crear instancia de modelo
                    var instance = originalMesh.createMeshInstance(originalMesh.Name + i + "_" + j);

                    var position = new Vector3(i * offset, 0, j * offset);
                    instance.AutoTransformEnable = false;
                    instance.Position = position;
                    instance.Scale = originalMesh.Scale;
                    instance.AlphaBlendEnable = originalMesh.AlphaBlendEnable;
                    //Desplazarlo
                    instance.Transform = Matrix.Scaling(instance.Scale) * Matrix.Translation(instance.Position) * instance.Transform;
                    //instance.Scale = new Vector3(0.25f, 0.25f, 0.25f);
                    meshes.Add(instance);
                }
            }
        }
        
        /// <summary>
        ///     Dispone un mesh en forma de linea recta n veces por el eje X
        /// </summary>
        /// <param name="orginalMesh">Mesh a disponer</param>
        /// <param name="lista">Lista del elemento a replicar. Debe estar instanciada</param>
        /// <param name="veces">Cantidad de veces/param>
        /// <param name="offset">Distancia entre los meshes/param>
        /// <param name="initPos">Posicion inicial de partida/param>
        public static void disponerEnLineaX(TgcMesh originalMesh, List<TgcMesh> meshes, int veces, float offset, Vector3 initPos)
        {
            for (var i = 0; i < veces; i++)
            {
                //Crear instancia de modelo
                var instance = originalMesh.createMeshInstance(originalMesh.Name  + meshes .Count + i);
                instance.AutoTransformEnable = false;

                var position = new Vector3(initPos.X + i * offset, initPos.Y, initPos.Z);
                instance.Position = position;
                //instance.Scale = originalMesh.Scale;
                instance.AlphaBlendEnable = true;
                //Desplazarlo
                instance.Transform = Matrix.Translation(instance.Position) * instance.Transform;

                meshes.Add(instance);
            }
        }        

        public static void disponerEnLineaZ(TgcMesh originalMesh, List<TgcMesh> meshes, int veces, float offset, Vector3 initPos)
        {
            for (var i = 0; i < veces; i++)
            {
                //Crear instancia de modelo
                var instance = originalMesh.createMeshInstance(originalMesh.Name + meshes.Count + i);

                var position = new Vector3(initPos.X, initPos.Y, initPos.Z + i * offset);

                instance.AutoTransformEnable = false;
                instance.Position = position;

                instance.AlphaBlendEnable = true;
                //Desplazarlo
                instance.Transform = Matrix.Translation(instance.Position) * instance.Transform;
                meshes.Add(instance);
            }
        }


        public static void disponerAleatorioXZ(TgcMesh originalMesh, List<TgcMesh> meshes, int veces)
        {

            //-16893, -2000, 17112
            var n = new Random();
            for (var i = 0; i < veces; i++)
            {
                //var x = n.Next(-160*8, 160*8);
                //var y = n.Next(-160*8, 160*8);
                var x = n.Next(-16890, 16890);
                var z = n.Next(-17112, 17112);

                var instance = originalMesh.createMeshInstance(originalMesh.Name + meshes.Count + 1);

                instance.AutoTransformEnable = false;
                instance.AlphaBlendEnable = true;

                instance.Position = new Vector3(x, 0, z);
                //instance.Scale = originalMesh.Scale;
                instance.Transform = Matrix.Translation(instance.Position) * instance.Transform;
                meshes.Add(instance);
            }
        } 


        public static void aleatorioXZExceptoRadioInicial(TgcMesh originalMesh, List<TgcMesh> meshes, int veces)
        {
            int radioCentro = 8000;

            var n = new Random();
            for (var i = 0; i < veces; i++)
            {
                var x = n.Next(-15927, 15927);
                var z = n.Next(-15112, 15112);

                //desplazo los objetos que se encuentran en el circulo del medio del mapa
                if (FastMath.Pow2(x) + FastMath.Pow2(z) < FastMath.Pow2(radioCentro))
                {
                    x = x * radioCentro;
                    z = z * radioCentro;
                }

                var instance = originalMesh.createMeshInstance(originalMesh.Name + meshes.Count + 1);

                instance.AutoTransformEnable = false;
                instance.AlphaBlendEnable = true;

                instance.Position = new Vector3(x, 0, z);
                instance.Transform = Matrix.Translation(instance.Position) * instance.Transform;
                meshes.Add(instance);
            }
        }

        /// <summary>
        ///     Renderiza todos los elementos de una lista de meshes.
        /// </summary>
        public static void renderMeshes(List<TgcMesh> meshes)
        {
            foreach(var mesh in meshes) mesh.render();
        }


        public static void renderFromFrustum(List<TgcMesh> meshes,TgcFrustum frustum)
        {
            foreach (var mesh in meshes)
            {
                var r = TgcCollisionUtils.classifyFrustumAABB(frustum, mesh.BoundingBox);
                if (r != TgcCollisionUtils.FrustumResult.OUTSIDE)
                {
                    mesh.render();
                }
            }
        }

        public static void applyTransform(List<TgcMesh> meshes, Matrix matriz)
        {
            foreach (var mesh in meshes)
            {
                mesh.AutoTransformEnable = false;
                mesh.Transform = matriz * mesh.Transform;
            }
        }

		public static float DegreeToRadian(float degree)
		{
			return degree * (3.141592654f / 180.0f);
		}
    }
}
