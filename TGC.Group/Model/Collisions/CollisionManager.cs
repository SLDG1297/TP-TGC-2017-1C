using Microsoft.DirectX;
using System;
using System.Collections.Generic;
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
        List<TgcBoundingAxisAlignBox> boundingBoxes = new List<TgcBoundingAxisAlignBox>();
        List<TgcBoundingCylinderFixedY> boundingCylinders = new List<TgcBoundingCylinderFixedY>();

        //objetos que cambian su posicion
        List<Bala> balas = new List<Bala>();
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
            var aSacar = new List<Bala>();
            var enemigosASacar = new List<Personaje>();

            //chequeo las colisiones de las balas que se esten moviendo
            if (balas.Count != 0)
            {
                foreach (var bala in balas)
                {
                    //actualizo la posicion de la bala
                    bala.update(ElapsedTime);

                    //si colisiona con un objeto, desaparece
                    if (colisionaConAAAB(bala.BoundingBox)) aSacar.Add(bala);
                    if (colisionaConCilindro(bala.BoundingBox)) aSacar.Add(bala);

                    //si colisiona con un Personaje, sacarle vida
                    var enemigo = enemigoQueColisionaCon(bala.BoundingBox);
                    if (enemigo != null)
                    {
                        aSacar.Add(bala);
                        //resto salud al que recibio el impacto de la bala
                        enemigo.recibiDanio(bala.Danio);                            

                            //si murio, lo saco del juego xd
                       if(enemigo.Muerto)enemigosASacar.Add(enemigo);                                               
                    }
                }
            }

            //remuevo las balas que colisionaron con algo
            foreach (var bala in aSacar)
            {
                bala.dispose();
                balas.Remove(bala);
            }

            //remuevo las balas que colisionaron con algo
            foreach (var enemigo in enemigosASacar)
            {
                enemigo.dispose();
                this.jugadores.Remove(enemigo);
            }
        }
        
        public void renderAll(float elapsedTime)
        {
            //renderizar personajes
            player.render(elapsedTime);
            foreach (var enemy in jugadores) enemy.render(elapsedTime);

            foreach (var bb in boundingBoxes) bb.render();
            foreach (var bb in boundingCylinders) bb.render();

            //renderizar balas
            if (balas.Count != 0)
            {
                foreach (var bala in balas)
                {
                    bala.render();
                    //TODO: BORRAR
                    bala.Mesh.BoundingBox.render();
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


        public Personaje enemigoQueColisionaCon(TgcBoundingAxisAlignBox aabb)
        {
            return jugadores.Find(
                enemigo => TgcCollisionUtils.testAABBCylinder(aabb, enemigo.BoundingCylinder)
                );
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
        public void setPlayer(Player player)
        {
            this.player = player;
        }

        public void addEnemy(Enemy enemy)
        {
            jugadores.Add(enemy);
        }

#endregion
    }
}
