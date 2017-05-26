using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.BoundingVolumes;
using TGC.Core.Collision;
using TGC.Core.Utils;
using TGC.Group.Model.Entities;

namespace TGC.Group.Model.Collisions
{
    public class CollisionManager
    {
        //Boundingboxes de objetos estaticos
        private List<TgcBoundingAxisAlignBox> boundingBoxes = new List<TgcBoundingAxisAlignBox>();
        private List<TgcBoundingCylinderFixedY> boundingCylinders = new List<TgcBoundingCylinderFixedY>();
        private List<TgcBoundingSphere> boundingSpheres = new List<TgcBoundingSphere>();
        private List<TgcBoundingOrientedBox> orientedBoxes = new List<TgcBoundingOrientedBox>();

        //objetos que cambian su posicion
        private List<Bala> balas = new List<Bala>();
        Player player;
        List<Personaje> jugadores = new List<Personaje>();

        /* Utilizamos el patron singleton, para que cada objeto pueda llamar al colisionador,
         * sin tener que pasarlo por atributo.
         * Se lo llama asi: CollisionManager.Instance.'metodo()'
         */
        private static CollisionManager instance;

        //Constructor privado para que nadie pueda instanciarlo
        private CollisionManager(){}

        public static CollisionManager Instance
        { get
            {
                if (instance == null) instance = new CollisionManager();      
                return instance;
            }
        }
        
        public void checkCollisions(float ElapsedTime)
        {
            //calculos de choque de balas, muerte de jugadores
            checkBulletCollisions(ElapsedTime);
        }
        
        
        public Vector3 adjustPosition(Personaje personaje, Vector3 desplazamiento)
        {
            var res1 = adjustAABBCollisions(personaje, desplazamiento);
            var res2 = adjustCylinderCollisions(personaje, res1);
            var res3 = adjustSphereCollisions(personaje,res2);
            return res3;
        }

        public Vector3 adjustAABBCollisions(Personaje personaje, Vector3 desplazamiento)
        {
            var res = new Vector3();
            var cylinder = personaje.BoundingCylinder;
            var y = desplazamiento.Y;

            //objetos que colisionan con el cilindro del jugador
            var obs = boundingBoxes.FindAll(
                boundingBox => TgcCollisionUtils.testAABBCylinder(boundingBox, cylinder));

            //interseccion de la direccion de movimiento del jugador con el cilindro
            var intersection = new Vector3();
            var lista = boundingBoxes.FindAll(
                bb => TgcCollisionUtils.intersectSegmentAABB(personaje.Position, desplazamiento, bb, out intersection));
            intersection.Y = 0;

            //si el cilindro esta colisionando con el aabb, rebotar
            if (obs.Count > 0)
            {                
                var dif = new Vector3();
                foreach (var aabb in obs)
                {
                    var centerAABB = aabb.calculateBoxCenter();
                    //determino el punto mas cercano del AABB al cilindro
                    var puntoqestorba = TgcCollisionUtils.closestPointCylinder(centerAABB, cylinder);
                    //distancia entre el punto del cilindro y su centro
                    dif = puntoqestorba - cylinder.Center;
                }

                if (lista.Count == 0)
                {
                    //si iba en direccion opuesta al aabb, me muevo en la direccion normal
                    res = -desplazamiento - dif;
                }
                else
                {
                    res = -dif;
                }
            }
            else
            {
                cylinder.setRenderColor(Color.Yellow);
                //si el vector desplazamiento resulta que intersecta con el aabb, rebotar
                if (lista.Count > 0)
                {
                    res = desplazamiento - intersection;
                }
                else
                {
                    //si no hay choque, moverse libremente
                    res = desplazamiento;
                }
            }

            res.Y = y;
            return res;
        }

        public Vector3 adjustCylinderCollisions(Personaje personaje, Vector3 desplazamiento)
        {
            var res = desplazamiento;
            var cylinder = personaje.BoundingCylinder;

            //objetos que colisionan con el cilindro del jugador
            var obs = boundingCylinders.FindAll(
                boundingCylinder => TgcCollisionUtils.testCylinderCylinder(boundingCylinder, cylinder));


            Vector3 intersection = new Vector3();

            var intersectCylinders = boundingCylinders.FindAll(
                boundingCylinder => intersectSegmentCylinder(desplazamiento,personaje.Position,  boundingCylinder, out intersection)
                );
            intersection.Y = 0;


            if (obs.Count > 0)
            {
                cylinder.setRenderColor(Color.Red);
                var dif = new Vector3();

                foreach (var cilindro in obs)
                {
                    var center = cilindro.Center;
                    //determino el punto mas cercano del AABB al cilindro
                    var puntoqestorba = TgcCollisionUtils.closestPointCylinder(center, cylinder);
                    //distancia entre el punto del cilindro y su centro
                    var distance = puntoqestorba - cylinder.Center;
                    dif = puntoqestorba - cylinder.Center;
                    //res -= distance;
                }

                if(intersectCylinders.Count == 0)
                {
                    //si iba en direccion opuesta al cilindro, me muevo en la direccion normal
                    res = -desplazamiento - dif;
                }
                else
                {
                    res = -dif;
                }
            }
            else
            {
                cylinder.setRenderColor(Color.Yellow);

                if(intersectCylinders.Count > 0)
                {
                    //si el vector desplazamiento resulta que intersecta con el cilindro, rebotar
                    res = desplazamiento - intersection;
                }
            }

            res.Y = desplazamiento.Y;
            return res;
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

        //TODO: Implementar!!
        public Vector3 adjustSphereCollisions(Personaje personaje, Vector3 desplazamiento)
        {
            Vector3 res = desplazamiento;
            return res;
        }

        //METODOS ASOCIADOS A LAS COLISIONES CON BALAS
        public void checkBulletCollisions(float ElapsedTime)
        { 
            var enemigosASacar = new List<Personaje>();

            //chequeo las colisiones de las balas que se esten moviendo
            if (balas.Count != 0)
            {
                foreach (var bala in balas)
                {
                    //actualizo la posicion de la bala
                    bala.update(ElapsedTime);

                    var boundingBox = bala.BoundingBox;

                    //si colisiona con un objeto, desaparece
                    if (colisionaConAAAB(boundingBox) || colisionaConCilindro(boundingBox)) bala.setImpacto(true); //aSacar.Add(bala);
                                        //si colisiona con un Personaje, sacarle vida
                    if (colisionaConJugador(boundingBox))
                    {
                        bala.setImpacto(true);
                        bala.BoundingBox.setRenderColor(Color.Red);
                        var jugador = enemigoQueColisionaCon(boundingBox);
                        jugador.recibiDanio(bala.Danio);

                        if (jugador.Muerto) enemigosASacar.Add(jugador);
                    }                   
                }
            }

            foreach (var enemigo in enemigosASacar)
            {
                enemigo.dispose();
                this.jugadores.Remove(enemigo);
            }
        }
        
        public void renderAll(float elapsedTime)
        {
            foreach (var bb in boundingBoxes) bb.render();
            foreach (var bb in boundingCylinders) bb.render();
            foreach (var bb in orientedBoxes) bb.render();

            foreach(var bb in jugadores)
            {
                bb.BoundingCylinder.render();
                bb.HeadCylinder.render();
            }
            //renderizar balas
            //remuevo primero las que impactaron
            balas.FindAll(bala => bala.Impacto == true).ForEach(bala => bala.dispose());
            balas.RemoveAll(bala => bala.Impacto == true);

            if (balas.Count > 0)
            {
                foreach (var bala in balas)
                {
                    if (bala.Impacto == false)
                    {
                        bala.render();
                        //TODO: BORRAR
                        bala.Mesh.BoundingBox.render();
                    }
                }
            }
        }
        
        public void disposeAll()
        {
            foreach (var enemy in jugadores) enemy.dispose();
            player.dispose();
            foreach (var bala in balas) bala.dispose();
        }

#region METODOS AUXILIARES
        public bool colisionaConAAAB(TgcBoundingAxisAlignBox aabb)
        {
            return boundingBoxes.Any(
                boundingBox => TgcCollisionUtils.testAABBAABB(boundingBox, aabb));
        }

        public bool colisionaConCilindro(TgcBoundingAxisAlignBox aabb)
        {
            return boundingCylinders.Any(
                boundingCylinder => TgcCollisionUtils.testAABBCylinder(aabb, boundingCylinder));
        }

        public bool colisionaConJugador(TgcBoundingAxisAlignBox aabb)
        {
            return jugadores.Any(
                jugador => TgcCollisionUtils.testAABBCylinder(aabb,jugador.BoundingCylinder));
        }


        public Personaje enemigoQueColisionaCon(TgcBoundingAxisAlignBox aabb)
        {
            return jugadores.Find(
                enemigo => TgcCollisionUtils.testAABBCylinder(aabb, enemigo.BoundingCylinder)
                );
        }

        public List<Bala> getBalas()
        {
            return balas;
        }

        public bool debeDisparar(Enemy enemigo)
        {
            var cilindro = player.BoundingCylinder;
            return TgcCollisionUtils.testRayCylinder(enemigo.Ray,cilindro);
        }

        public void agregarAABB(TgcBoundingAxisAlignBox a)
        {
            boundingBoxes.Add(a);
        }

        public void agregarCylinder(TgcBoundingCylinderFixedY c)
        {
            boundingCylinders.Add(c);
        }

        public void agregarOBB(TgcBoundingOrientedBox obb)
        {
            orientedBoxes.Add(obb);
        }


        public void agregarBala(Bala bala)
        {
            balas.Add(bala);
        }
        
        public void cleanAll()
        {
            boundingBoxes.Clear();
            boundingCylinders.Clear();
        }


        //TODO: Borrar
        public void setPlayer(Player p)
        {
            player = p;
            jugadores.Add(p);
        }

        public void addEnemy(Enemy enemy)
        {
            jugadores.Add(enemy);
        }

#endregion
    }
}
