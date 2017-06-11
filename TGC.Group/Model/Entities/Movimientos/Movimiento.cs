using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            System.Console.Out.WriteLine(dist);

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
            var ret = new Vector3(0, 0, 0);
            return ret * signo;
        }

        public override void updateStatus(Enemy enemigo, Vector3 posicionJugador)
        {
            if(estaCerca(enemigo,posicionJugador))
            {
                enemigo.setEstado(new Perseguir(false));
            }
        }
    }

    //EL ENEMIGO SE MUEVE EN DIAGONAL
    public class Diagonal : Movimiento
    {
        public override Vector3 mover(Enemy enemigo, Vector3 posicionJugador)
        {
            return new Vector3(1, 0, 1) * enemigo.VelocidadCaminar * signo;
        }

        public override void updateStatus(Enemy enemigo, Vector3 posicionJugador)
        {
        }
    }

    //EL ENEMIGO SE MUEVE SOBRE UN EJE
    public class Unidireccion : Movimiento
    {
        private int x = 1;
        private int z = 0;

        public override Vector3 mover(Enemy enemigo, Vector3 posicionJugador)
        {
            return new Vector3(x, 0, z) * enemigo.VelocidadCaminar *  signo;
        }

        public override void invertirDireccion()
        {
            if (z == 0)
            {
                z = 1;
                x = 0;
            }

            if (x == 0)
            {
                z = 0;
                x = 1;
            }


        }

        public override void updateStatus(Enemy enemigo, Vector3 posicionJugador)
        {
            if (CollisionManager.Instance.colisiona(enemigo))
            {
                invertirDireccion();
            }

            if (estaCerca(enemigo, posicionJugador))
            {
                enemigo.setEstado(new Perseguir(false));
            }
        }
    }

     //EL ENEMIGO SE MUEVE HASTA LA POSICION DEL ENEMIGO
     public class Perseguir : Movimiento
     {
        public Perseguir(bool persiguiendo)
        {
            if (persiguiendo) signo = -1;
            else signo = 1;
        }

        public override Vector3 mover(Enemy enemigo, Vector3 posicionJugador)
        {
            var res = enemigo.Position - posicionJugador;

            return res * signo;
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
        }
     }
}
