using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.BoundingVolumes;
using TGC.Core.Collision;
using TGC.Core.Geometry;
using TGC.Group.Model.Entities;
using TGC.Group.Model.Environment;

namespace TGC.Group.Model.Collisions
{
    public class CollisionUtils
    {
        /// <summary>
        ///     Indica si un AABB colisiona con algun elemento de la lista de AABB
        /// </summary>
        public static bool colisionaConAAAB(TgcBoundingAxisAlignBox aabb, List<TgcBoundingAxisAlignBox> boundingBoxes)
        {
            return boundingBoxes.Any(
                boundingBox => TgcCollisionUtils.testAABBAABB(boundingBox, aabb));
        }

        /// <summary>
        ///     Indica si un AABB colisiona con algun elemento de la lista de cilindros
        /// </summary>
        public static bool colisionaConCilindro(TgcBoundingAxisAlignBox aabb, List<TgcBoundingCylinderFixedY> boundingCylinders)
        {
            return boundingCylinders.Any(
                boundingCylinder => TgcCollisionUtils.testAABBCylinder(aabb, boundingCylinder));
        }

        /// <summary>
        ///     Indica si un AABB colisiona con el bounding cylinder de un personaje
        /// </summary>
        public static bool colisionaConJugador(TgcBoundingAxisAlignBox aabb, List<Personaje> jugadores)
        {
            return jugadores.Any(
              jugador => TgcCollisionUtils.testAABBCylinder(aabb, jugador.BoundingCylinder));

        }

        /// <summary>
        ///     Indica si una bala colisiona con el bounding cylinder de un personaje
        /// </summary>
        public static bool colisionaConJugador(Bala bala, List<Personaje> jugadores)
        {

            return jugadores.Any(
                jugador => colisionaCon(bala, jugador));
        }

        /// <summary>
        ///     Indica si un AABB colisiona con el bounding cylinder de un barril
        /// </summary>
        public static bool colisionaConBarril(TgcBoundingAxisAlignBox aabb, List<Barril> barriles)
        {
            return barriles.Any(
                barril => TgcCollisionUtils.testAABBCylinder(aabb, barril.BoundingCylinder));
        }

        /// <summary>
        ///     Devuelve aquellos que colisionan con un AABB
        /// </summary>
        public static Personaje enemigoQueColisionaCon(TgcBoundingAxisAlignBox aabb, List<Personaje> jugadores)
        {
            return jugadores.Find(
                enemigo => TgcCollisionUtils.testAABBCylinder(aabb, enemigo.BoundingCylinder)
                );
        }

        /// <summary>
        ///     Devuelve aquellos que colisionan con una Bala
        /// </summary>
        public static Personaje enemigoQueColisionaCon(Bala bala, List<Personaje> jugadores)
        {
            return jugadores.Find(
                enemigo => colisionaCon(bala, enemigo)
                );
        }

        public static bool colisionaCon(Bala bala, Personaje personaje)
        {
            return testPointCylinder(bala.Mesh.Position, personaje.BoundingCylinder);
        }

        public static bool testPointCylinder(Vector3 p, TgcBoundingCylinderFixedY cilindro)
        {
            //cilindro auxiliar con los mismos valores que el cilindro recibido como parametro
            //porque el framework no tiene un metodo que trabaje con cilindros orientados en Y
            TgcBoundingCylinder cil = new TgcBoundingCylinder(cilindro.Center, cilindro.Radius, cilindro.HalfLength);
            cil.Rotation = new Vector3(0, 0, 0);

            return TgcCollisionUtils.testPointCylinder(p, cil); ;
        }

        public bool intersectSegmentCylinder(Vector3 segmentInit, Vector3 segmentEnd, TgcBoundingCylinderFixedY cilindro, out Vector3 intersection)
        {
            float time;
            Vector3 interseccion = new Vector3();
            //cilindro auxiliar con los mismos valores que el cilindro recibido como parametro
            //porque el framework no tiene un metodo que trabaje con cilindros orientados en Y
            TgcBoundingCylinder cil = new TgcBoundingCylinder(cilindro.Center, cilindro.Radius, cilindro.HalfLength);
            cil.Rotation = new Vector3(0, 0, 0);

            bool resultado = TgcCollisionUtils.intersectSegmentCylinder(segmentInit, segmentEnd, cil, out time, out interseccion);

            intersection = interseccion;
            return resultado;
        } 

        public bool colisionBarrilBala(List<Bala> balas, Barril barril)
        {
            return balas.Any(bala => TgcCollisionUtils.testAABBCylinder(bala.BoundingBox, barril.BoundingCylinder));
        }
    }
}
