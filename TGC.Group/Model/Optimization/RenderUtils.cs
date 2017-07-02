using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.BoundingVolumes;
using TGC.Core.Collision;
using TGC.Core.SceneLoader;
using TGC.Group.Model.Entities;
using TGC.Group.Model.Environment;

namespace TGC.Group.Model.Optimization
{
    public class RenderUtils
    {
        /// <summary>
        ///     Renderiza todos los elementos de una lista de meshes.
        /// </summary>
        public static void renderMeshes(List<TgcMesh> meshes)
        {
            foreach (var mesh in meshes) mesh.render();
        }

        public static bool estaDentroDelFrustum(TgcMesh mesh, TgcFrustum frustum)
        {
            var r = TgcCollisionUtils.classifyFrustumAABB(frustum, mesh.BoundingBox);
            return r != TgcCollisionUtils.FrustumResult.OUTSIDE;
        }

        /// <summary>
        ///     Renderiza todos los elementos de una lista de meshes dado un frustum.
        /// </summary>
        public static void renderFromFrustum(List<TgcMesh> meshes, TgcFrustum frustum)
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

        public static void renderFromFrustum(List<Bala> balas, TgcFrustum frustum)
        {
            if (balas.Count > 0)
            {
                foreach (var bala in balas)
                {
                    var r = TgcCollisionUtils.classifyFrustumAABB(frustum, bala.BoundingBox);
                    if (r != TgcCollisionUtils.FrustumResult.OUTSIDE)
                    {
                        bala.render();
                    }
                }
            }
        }


        public static void renderFromFrustum(List<Personaje> enemigos, TgcFrustum frustum, float elapsedTime)
        {
            foreach (var enemigo in enemigos)
            {
                var r = TgcCollisionUtils.classifyFrustumAABB(frustum, enemigo.BoundingBox);
                if (r != TgcCollisionUtils.FrustumResult.OUTSIDE)
                {
                    enemigo.render(elapsedTime);
                }
            }
        }

        public static void renderFromFrustum(List<Barril> barriles, TgcFrustum frustum, float elapsedTime)
        {
            if (barriles.Count > 0)
            {
                foreach (var barril in barriles)
                {
                    var r = TgcCollisionUtils.classifyFrustumAABB(frustum, barril.Mesh.BoundingBox);
                    if (r != TgcCollisionUtils.FrustumResult.OUTSIDE)
                    {
                        barril.render(elapsedTime);

                    }
                }
            }
        }
    }
}
