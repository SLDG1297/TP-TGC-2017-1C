using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.SkeletalAnimation;
using TGC.Core.BoundingVolumes;
using TGC.Group.Model.Collisions;
using TGC.Core.Geometry;
using TGC.Core.Utils;
using TGC.Group.Model.Entities.Movimientos;

namespace TGC.Group.Model.Entities
{
	public class Enemy : Personaje
	{
		// Setear estatus de la IA. 0 = Parado, 1 = escapando, 2 = persiguiendo al jugador
		private float Rotacion = 0f;
        private Movimiento movimiento;
        private Vector3 direccion;
        private Vector3 direccion_disparo;

        private TgcRay ray;
        private TgcArrow arrow;
            
        /// <summary>
        ///     Construye un enemigo que trata de encontrar al jugador y matarlo.
        /// </summary>
        /// <param name="mediaDir">Ruta donde esta la carpeta con los assets</param>
        /// <param name="skin">Nombre del skin a usar (ej: CS_Gign, CS_Arctic)</param>
        /// <param name="skin">Posicion inicial del jugador</param>
        /// <param name="arma">Arma con la que el jugador comienzar</param> 
        public Enemy(string mediaDir, string skin, Vector3 initPosition, Arma arma) : base(mediaDir, skin, initPosition, arma)
		{
			maxHealth = 100;

			velocidadCaminar = 100f;
			velocidadIzqDer = 50f;
			velocidadRotacion = 120f;
			tiempoSalto = 10f;
			velocidadSalto = 0.5f;

            direccion = initPosition - new Vector3(initPosition.X, initPosition.Y, initPosition.Z + 1);
            //direccion.Normalize();
            direccion = direccion_disparo;
            var random = new Random();
            var num = random.Next(0,1);

            switch (num)
            {
                case 0:
                    movimiento = new Unidireccion(true,true);
                    break;
                case 1:
                    movimiento = new Diagonal(true,true);
                    break;
                //default:
                  //  movimiento = new Parado();
                    //break;
            }           

            ray = new TgcRay(initPosition + new Vector3(0,50,0),direccion);
            arrow = new TgcArrow();

           //var ray1;
           //var ray2;

            arrow.PStart = ray.Origin;
            arrow.PEnd = initPosition + (direccion * 100f);
            arrow.Thickness = 2f;
            arrow.HeadSize = new Vector2(2, 2);
        }

		public bool isCollidingWithObject(List<TgcBoundingAxisAlignBox> obstaculos) { 
			 var collider = getColliderAABB(obstaculos);
			return collider != null;
		}

        public void mover(Vector3 posicionJugador, float elapsedTime)
        {
            resetBooleans();

            var aux = direccion;
            //actualizo el tipo de movimiento
            movimiento.updateStatus(this, posicionJugador);

            //calculo el desplazamiento segun el tipo de movimiento
            var desplazamiento = movimiento.mover(this, posicionJugador) * elapsedTime;

            float rotation = 0f;
            moving = desplazamiento != new Vector3(0, 0, 0);
            esqueleto.AutoTransformEnable = false;

            //calculo el angulo de rotacion -ESTE ES EL QUE ESTA BIEN! NO CAMBIAR!!
            rotation = Utils.anguloEntre(Utils.proyectadoY(direccion), Utils.proyectadoY(desplazamiento));
            //roto
            esqueleto.rotateY(rotation);
            var realmovement = CollisionManager.Instance.adjustPosition(this, desplazamiento);
            esqueleto.Position += realmovement;

            CollisionManager.Instance.applyGravity(elapsedTime, this);
            if (moving)
            {
                direccion_disparo = Vector3.Normalize(Position - lastPos);
            }
            displayAnimations();

            updateBoundingBoxes();
            lastPos = esqueleto.Position;
            esqueleto.Transform = Matrix.RotationY(rotation)
                                  * Matrix.Translation(esqueleto.Position);

            arma.setPosition(esqueleto.Position);
            if (debeDisparar())
            {

                var angulodisparo = Utils.anguloEntre(Utils.proyectadoY(direccion_disparo), Utils.proyectadoY(desplazamiento));

                if (arma.Balas <= 0) arma.recarga();
                //arma.dispara(elapsedTime, Position, rotation);
                arma.dispara(elapsedTime, Position, angulodisparo);
            }

            updateRay();
        }
        

        public void updateRay()
        {            
            //el mismo que la bala!
            ray.Origin = esqueleto.Position + new Vector3(0, 40, 0);
            //var dir = new Vector3(direccion.X, direccion.Y, direccion.Z);

            var dir = new Vector3(direccion_disparo.X, direccion_disparo.Y, direccion_disparo.Z);

            //dir.Normalize();
            ray.Direction = dir;

            arrow.PStart = ray.Origin;
            arrow.PEnd = ray.Origin + direccion_disparo * 100f;
            arrow.updateValues();
        }

        public void setEstado(Movimiento movimiento)
        {
            this.movimiento = movimiento;            
        }

        public TgcRay Ray
        {
            get { return ray; }
        }

        public bool debeDisparar()
        {
            return
                CollisionManager.Instance.colisionRayoPlayer(ray);
        }

        public float VelocidadCaminar
        {
            get { return velocidadCaminar; }
        }

        public override void render(float elapsedTime) {
            esqueleto.animateAndRender(elapsedTime);
            //arrow.render();
        }
	}   
}
