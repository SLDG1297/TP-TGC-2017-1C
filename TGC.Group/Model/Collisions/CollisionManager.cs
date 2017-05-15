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
        
        
        public void adjustPosition(Personaje personaje)
        {
            checkAABBCollisions(personaje);
            checkCylinderCollisions(personaje);
            //checkSphereCollision(personaje);
        }


        private void checkAABBCollisions(Personaje personaje)
        {
            var cylinder = personaje.BoundingCylinder;
            //objetos que colisionan con el cilindro del jugador
            var obs = boundingBoxes.FindAll(
                boundingBox => TgcCollisionUtils.testAABBCylinder(boundingBox, cylinder));

            if (obs.Count != 0)
            {
                foreach (var aabb in obs)
                {
                    //TODO: POR AHORA SOLO RESUELVE COLISIONES EN EL EJE XZ
                    var centerAABB = aabb.calculateBoxCenter();
                    aabb.setRenderColor(System.Drawing.Color.Red);

                    //determino el punto mas cercano del AABB al cilindro
                    var puntoqestorba = TgcCollisionUtils.closestPointCylinder(centerAABB, cylinder);
                    
                    //distancia entre el punto del cilindro y su centro
                    var dif = puntoqestorba - cylinder.Center;
                    //desplazo al jugador y al cylindro EN EL EJE XZ
                    personaje.moveCylindersXZ(-dif);
                    Vector3 newPos = new Vector3(cylinder.Center.X, personaje.Esqueleto.Position.Y, cylinder.Center.Z);
                    personaje.Esqueleto.Position = newPos;
                }
            }
            else
            {
                //TODO: Borrar, esto es solo para testeo. Pinta los AABB de color amarillo cuando no colisionan
                var a = boundingBoxes.FindAll(
                 boundingBox => !TgcCollisionUtils.testAABBCylinder(boundingBox, cylinder));
                foreach (var c in a) c.setRenderColor(System.Drawing.Color.Yellow);
            }
        }

        public void checkCylinderCollisions(Personaje personaje)
        {
            var cylinder = personaje.BoundingCylinder;

            //objetos que colisionan con el cilindro del jugador
            var obs = boundingCylinders.FindAll(
                boundingCylinder => TgcCollisionUtils.testCylinderCylinder(boundingCylinder, cylinder));

            foreach (var cilindro in obs)
            {
                //TODO: POR AHORA SOLO RESUELVE COLISIONES EN EL EJE XZ
                var center = cilindro.Center;

                //determino el punto mas cercano del AABB al cilindro
                var puntoqestorba = TgcCollisionUtils.closestPointCylinder(center, cylinder);
                
                //distancia entre el punto del cilindro y su centro
                var dif = puntoqestorba - cylinder.Center;

                //desplazo al jugador y al cylindro EN EL EJE XZ
                personaje.moveCylindersXZ(-dif);
                Vector3 newPos = new Vector3(cylinder.Center.X, personaje.Esqueleto.Position.Y, cylinder.Center.Z);
                personaje.Esqueleto.Position = newPos;
            }
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
