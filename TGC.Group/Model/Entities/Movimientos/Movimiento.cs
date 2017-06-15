using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.BoundingVolumes;
using TGC.Core.Utils;
using TGC.Group.Model.Collisions;

namespace TGC.Group.Model.Entities.Movimientos
{
    public abstract class Movimiento
    {
        protected int signo = 1;
        public virtual void invertirDireccion()
        {
            signo = signo * (-1);
        }

        public abstract void updateStatus(Enemy enemigo, Vector3 posicionJugador);

        public abstract Vector3 mover(Enemy enemigo, Vector3 posicionJugador);
        
        public bool estaLejos(Enemy enemigo, Vector3 posicionJugador)
        {
            var dir_escape = enemigo.Position - posicionJugador;
            dir_escape.Y = 0;
            var dist = dir_escape.Length();

            return dist >= 400;
        }

        public bool estaCerca(Enemy enemigo, Vector3 posicionJugador)
        {
            var dir_escape = enemigo.Position - posicionJugador;
            dir_escape.Y = 0;
            var dist = dir_escape.Length();

            return dist <= 250;
        }
    }

    //EL ENEMIGO SE QUEDA QUIETO
    public class Parado : Movimiento
    {
        public override Vector3 mover(Enemy enemigo, Vector3 posicionJugador)
        {
            return new Vector3(0, 0, 0);
        }

        public override void updateStatus(Enemy enemigo, Vector3 posicionJugador)
        {
            //si esta a una distancia cercana, escapar para mantener una distancia segura
            if(estaCerca(enemigo,posicionJugador))
            {
                enemigo.setEstado(new Escapar());
            }

            //si el jugador se cruza en la mirada, disparar
            if(enemigo.debeDisparar())
            {
                enemigo.setEstado(new Perseguir());
            }

            //si esta muy lejos, moverse en una direccion cualquiera, o sino ir a buscarlo
        }
    }

    //EL ENEMIGO SE MUEVE EN DIAGONAL
    public class Diagonal : Movimiento
    {
        public Vector3 movimiento = new Vector3(1, 0, 1);

        public Diagonal(bool xPos, bool zPos)
        {
            if (!xPos) movimiento.X = -1;
            if (!zPos) movimiento.Z = -1;
        }

        public override Vector3 mover(Enemy enemigo, Vector3 posicionJugador)
        {
            return movimiento * enemigo.VelocidadCaminar * signo;
        }

        public override void updateStatus(Enemy enemigo, Vector3 posicionJugador)
        {
            //si choca con algun objeto, invertir direccion o cambiar de movimiento
            if (CollisionManager.Instance.colisiona(enemigo))
            {          
                //invertir direccion
                invertirDireccion();

                //seguir asi o sobre alguno de los ejes
                var random = new Random();
                var num = random.Next(0, 2);

                switch (num)
                {
                    //direccion de x
                    case 0:
                        enemigo.setEstado(new Unidireccion(true, movimiento.X > 0));
                        break;

                    //direccion de z
                    case 1:
                        enemigo.setEstado(new Unidireccion(false, movimiento.Z > 0));
                        break;

                    default:
                        //simplemente invierto la direccion
                        break;                        
                }
            }

            //si esta a una distancia cercana, escapar para mantener una distancia segura
            if (estaCerca(enemigo, posicionJugador))
            {
                enemigo.setEstado(new Escapar());
            }

            if(enemigo.debeDisparar() && estaLejos(enemigo, posicionJugador))
            {
                enemigo.setEstado(new Perseguir());
            }
        }
    }

    //EL ENEMIGO SE MUEVE SOBRE UN EJE
    public class Unidireccion : Movimiento
    {
        private int x = 1;
        private int z = 0;
        

        public Unidireccion(bool ejex, bool positivo)
        {
            signo = 1;
            if (!ejex)
            {
                x = 0;
                if (positivo) z = 1;
                else z = -1;
            }
            else
            {
                z = 0;
                if (positivo) x = 1;
                else x = -1;
            }
        }

        public override Vector3 mover(Enemy enemigo, Vector3 posicionJugador)
        {
            return new Vector3(x, 0, z) * enemigo.VelocidadCaminar *  signo;
        }
        

        public override void updateStatus(Enemy enemigo, Vector3 posicionJugador)
        {

            //si choca con algun objeto, invertir direccion o cambiar de movimiento
            if (CollisionManager.Instance.colisiona(enemigo))
            {
                invertirDireccion();

                //diagonal, pero siguiendo la direccion que tenia
                //seguir asi o sobre alguno de los ejes
                var random = new Random();
                var num = random.Next(0, 2);

                switch (num)
                {
                    case 0:
                        //uno de los signos positivo y el otro segun la direccion que tenia pero opuesta
                        if (x == 0)
                        {
                            enemigo.setEstado(new Diagonal(true, z > 0));
                        }
                        else
                        {
                            enemigo.setEstado(new Diagonal(z > 0, true));
                        }
                        break;

                    //direccion de z
                    case 1:
                        //lo opuesto a lo anterior
                        if (x == 0)
                        {
                            enemigo.setEstado(new Diagonal(false, z > 0));
                        }
                        else
                        {
                            enemigo.setEstado(new Diagonal(z > 0, false));
                        }
                        break;

                    default:
                        //simplemente invierto la direccion
                        break;
                }

                //invertir direccion
            }

            //si esta a una distancia cercana, escapar para mantener una distancia segura
            if (estaCerca(enemigo, posicionJugador))
            {
                enemigo.setEstado(new Escapar());
            }
        }
    }

     //EL ENEMIGO SE MUEVE HASTA LA POSICION DEL ENEMIGO
     public class Perseguir : Movimiento
     {
        public override Vector3 mover(Enemy enemigo, Vector3 posicionJugador)
        {
            var res = posicionJugador - enemigo.Position;
            return res;
        }

        public override void updateStatus(Enemy enemigo, Vector3 posicionJugador)
        {
            //por ahora (modificar pronto) se queda quieto, lo cual tiene sentido 
            //no va a escapar a otro lado sino que va a asegurarse de que tenga distancia apropiada para disparar
            //a lo sumo moverse en una direccion

            //EDIT : otra opcion es que busque cobertura
            if (estaLejos(enemigo, posicionJugador))
            { 
              enemigo.setEstado(new Parado());                
            }

            if (estaCerca(enemigo, posicionJugador))
            {
               //enemigo.setEstado(new Escapar());
            }
        }
     }

     public class Escapar : Movimiento
     {
        public override Vector3 mover(Enemy enemigo, Vector3 posicionJugador)
        {
            return enemigo.Position - posicionJugador;
        }

        public override void updateStatus(Enemy enemigo, Vector3 posicionJugador)
        {
            //por ahora (modificar pronto) se queda quieto, lo cual tiene sentido 
            //no va a escapar a otro lado sino que va a asegurarse de que tenga distancia apropiada para disparar
            //a lo sumo moverse en una direccion

            //EDIT : otra opcion es que busque cobertura
            if (estaLejos(enemigo, posicionJugador))
            {
                //enemigo.setEstado(new Parado());
                enemigo.setEstado(new Refugiarse());
            }
        }
     }
          
     public class Refugiarse : Movimiento
     {
        public bool cubierto;

        public override Vector3 mover(Enemy enemigo, Vector3 posicionJugador)
        {
            //busco objetos a la redonda para cubrirme
            //de ellos, me escondo detras del que este mas cerca del jugador
            //para esconderme, me muevo hasta que haya objeto entre el jugador y el enemigo
            var res = new Vector3();
            var res1 = new Vector3(0, 0, 0);
            float dist1 = 0;

            var res2 = new Vector3(0, 0, 0);
            float dist2= 0;

            var cilindros = CollisionManager.Instance.boundingCylindersDentroDelRadio(enemigo, 700);
            var boundingBoxes = CollisionManager.Instance.boundingBoxDentroDelRadio(enemigo, 700);

            if(cilindros != null)
            {
                var cilindroMasCercano = CollisionManager.Instance.cilindroMasCercano(enemigo, cilindros);

                if(cilindroMasCercano!= null)
                {
                    //si se interpone algo entre el jugador y el enemigo
                    //sumarle algo en el sentido
                    res1 = Vector3.Subtract( cilindroMasCercano.Center, enemigo.Position);
                    dist1 = res1.Length();
                }
                else
                {
                    res1 = new Vector3(0, 0, 0);
                }
            }

            if (boundingBoxes != null)
            {
                var bbMasCercano = CollisionManager.Instance.AABBMasCercano(enemigo, boundingBoxes);

                    if (bbMasCercano != null)
                    {
                        //si se interpone algo entre el jugador y el enemigo
                        //sumarle algo en el sentido
                        res2 = Vector3.Subtract(bbMasCercano.calculateBoxCenter(), enemigo.Position);
                        dist2 = res2.Length();
                    }
                    else
                    {
                         res2 = new Vector3(0, 0, 0);
                    }
            }

            if (dist1 == 0)
            {
                if (dist2 != 0)
                {
                    res = res2;
                }
            }
            else
            {
                if(dist2 == 0)
                {
                    res = res1;
                }
            }

            res.Y = 0;
            return res;
        }

        public override void updateStatus(Enemy enemigo, Vector3 posicionJugador)
        {
            if (estaCerca(enemigo, posicionJugador))
            {
                enemigo.setEstado(new Escapar());
            }

            if (CollisionManager.Instance.colisiona(enemigo))
            {
                enemigo.setEstado(new Parado());
            }
        }

        
     }
}