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

        public static void disponerEnLineaX(TgcMesh originalMesh, List<TgcMesh> meshes, int veces, float offset)
        {
            for (var i = 0; i < veces; i++)
            {
                //Crear instancia de modelo
                var instance = originalMesh.createMeshInstance(originalMesh.Name + i);

                var position = new Vector3(originalMesh.Position.X + i * offset, originalMesh.Position.Y, originalMesh.Position.Z);

                instance.AutoTransformEnable = false;
                instance.Position = position;
                instance.Scale = originalMesh.Scale;

                instance.AlphaBlendEnable = true;
                //Desplazarlo
                instance.Transform = Matrix.Scaling(instance.Scale) * Matrix.Translation(instance.Position) * instance.Transform;
                
                meshes.Add(instance);
            }
        }


        public static void disponerEnLineaZ(TgcMesh originalMesh, List<TgcMesh> meshes, int veces, float offset)
        {
            for (var i = 0; i < veces; i++)
            {
                //Crear instancia de modelo
                var instance = originalMesh.createMeshInstance(originalMesh.Name + i);

                var position = new Vector3(originalMesh.Position.X, originalMesh.Position.Y, originalMesh.Position.Z + i * offset);

                instance.AutoTransformEnable = false;
                instance.Position = position;

                instance.AlphaBlendEnable = true;
                //Desplazarlo
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
