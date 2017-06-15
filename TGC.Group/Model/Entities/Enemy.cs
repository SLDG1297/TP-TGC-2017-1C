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
            
            var random = new Random();
            var num = random.Next(1, 3);

            switch (num)
            {
                case 0:
                    movimiento = new Unidireccion(true,true);
                    break;
                case 1:
                    movimiento = new Diagonal(true,true);
                    break;
                default:
                    movimiento = new Parado();
                    break;
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

        public void mover(Vector3 posicionJugador, List<TgcBoundingAxisAlignBox> obstaculos, float elapsedTime, float posicionY)
        {
            movimiento.updateStatus(this, posicionJugador);
            var desplazamiento = movimiento.mover(this, posicionJugador) * elapsedTime;
            resetBooleans();

            float rotation = 0f;

            moving = desplazamiento != new Vector3(0, 0, 0);
            //rotate = 0f;

            //esqueleto.AutoTransformEnable = false;
            //if (moving)
            //{
            //    rotation = anguloEntre(desplazamiento, direccion);
            //    esqueleto.rotateY(rotation);
            //}

            displayAnimations();

            desplazamiento.TransformCoordinate(Matrix.RotationY(Rotacion));
            var realmovement = CollisionManager.Instance.adjustPosition(this, desplazamiento);
            esqueleto.Position += realmovement;
            CollisionManager.Instance.applyGravity(elapsedTime,this);
            //direccion = realmovement;

            updateBoundingBoxes();
            lastPos = esqueleto.Position;
            esqueleto.Transform = Matrix.RotationY(rotation) 
                                  * Matrix.Translation(esqueleto.Position);

            arma.setPosition(esqueleto.Position);
            //actualizo el rayo);
            if (debeDisparar())
            {
                if (arma.Balas <= 0) arma.recarga();
                arma.dispara(elapsedTime, Position, esqueleto.Rotation.Y);
            }

            updateRay();
        }

        public float anguloEntre(Vector3 v1, Vector3 v2)
        {
            var vector1 = new Vector3(v1.X, 0, v1.Z);
            var vector2 = new Vector3(v2.X, 0, v2.Z);
            var angle = FastMath.Acos(Vector3.Dot(Vector3.Normalize(vector1), Vector3.Normalize(vector2)));

            return angle;
        }

        public void updateRay()
        {
            //el mismo que la bala!
            ray.Origin = esqueleto.Position + new Vector3(0, 40, 0);
            var dir = new Vector3(direccion.X, direccion.Y, direccion.Z);
            //dir.Normalize();
            ray.Direction = dir;

            arrow.PStart = ray.Origin;
            arrow.PEnd = ray.Origin + direccion * 100f;
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
            arrow.render();
        }
	}   
}
