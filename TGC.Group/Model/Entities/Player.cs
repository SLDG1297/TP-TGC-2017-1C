using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.SkeletalAnimation;
using TGC.Core.Input;
using TGC.Core.Geometry;
using Microsoft.DirectX.DirectInput;
using TGC.Core.Collision;
using TGC.Core.BoundingVolumes;
using Microsoft.DirectX;
using TGC.Core.Utils;

namespace TGC.Group.Model.Entities
{
    public class Player : Personaje
    {
        private Vector3 lastPos; // Ultima posicion
		private float Rotacion = 0f;

        /// <summary>
        ///     Construye un jugador manejado por el usuario (WASD).
        /// </summary>
        /// <param name="mediaDir">Ruta donde esta la carpeta con los assets</param>
        /// <param name="skin">Nombre del skin a usar (ej: CS_Gign, CS_Arctic)</param>
        /// <param name="skin">Posicion inicial del jugador</param>
        /// <param name="arma">Arma con la que el jugador comienzar</param> 
        public Player(string mediaDir, string skin,Vector3 initPosition, Arma arma) :base(mediaDir, skin, initPosition, arma) {

            velocidadCaminar = 100f;
            velocidadIzqDer = 100f;
            velocidadRotacion = 20f;
            tiempoSalto = 10f;
            velocidadSalto = 0.5f;
            resetBooleans();
			lastPos = initPosition;
        }

        public void recuperaSalud(int salud){
            if (salud + health > maxHealth)
            {
                health = maxHealth;
            }
            else
            {
                health += salud;
            }
        }        

		public void mover(TgcD3dInput Input, float ElapsedTime, List<TgcBoundingAxisAlignBox> obstaculos) {
            //Calcular proxima posicion de personaje segun Input
            var moveForward = 0f;
            var moveLeftRight = 0f;

            float jump = 0;
			float jumpingElapsedTime = 0f;
            float rotate = 0;

            resetBooleans();

			// Rotar respecto a la posicion del mouse
			Rotacion += Input.XposRelative * 0.05f;

            //Correr
            if (running = Input.keyDown(Key.LeftShift)){
                setVelocidad(200f, 200f);
            }
            else
            {
                //Agacharse
                if (crouching = Input.keyDown(Key.LeftControl))
                {
                    setVelocidad(40f, 40f);
                }
                else
                {
                    setVelocidad(100f, 100f);
                }
            }

            //Adelante
            if (Input.keyDown(Key.W))
            {
                moveForward = -velocidadCaminar;
                moving = true;
            }

            //Atras
            if (Input.keyDown(Key.S))
            {
				moveForward = velocidadCaminar - 10f;
                moving = true;
				rotate = 180;
            }

            //Derecha
            if (Input.keyDown(Key.D))
            {
                moveLeftRight = -velocidadIzqDer;
				//rotate = velocidadRotacion;
				rotate += Input.keyDown(Key.W) ? 45 : Input.keyDown(Key.S) ? 315 : 90;
                rotating = true;
                moving = true;
            }

            //Izquierda
            if (Input.keyDown(Key.A))
            {
                moveLeftRight = velocidadIzqDer;
				//rotate = -velocidadRotacion;
				rotate = Input.keyDown(Key.W) ? -45 : Input.keyDown(Key.S) ? -135 : -90;
				rotating = true;
                moving = true;
            }

            //Saltar
            if (!jumping && Input.keyPressed(Key.Space))
            {
                jumping = true;
            }

            //Recargar
            if (Input.keyPressed(Key.R))
            {
                arma.recarga();
            }

            displayAnimations();

            //Actualizar salto
            if (jumping)
            {
                esqueleto.playAnimation("Jump", true);
                //El salto dura un tiempo hasta llegar a su fin
                jumpingElapsedTime += ElapsedTime;
                if (jumpingElapsedTime > tiempoSalto)
                {
                    jumping = false;
                }
                else
                {
                    jump = velocidadSalto * (tiempoSalto - jumpingElapsedTime);
                }
            }

			var desplazamiento = new Vector3(moveLeftRight * ElapsedTime, jump, moveForward * ElapsedTime);

			desplazamiento.TransformCoordinate(Matrix.RotationY(Rotacion));

			esqueleto.Position += desplazamiento;

			esqueleto.Transform = Matrix.RotationY(Utils.DegreeToRadian(rotate))
								* Matrix.RotationY(Rotacion)
								* Matrix.Translation(esqueleto.Position);
            //Disparar
            if (Input.buttonPressed(TgcD3dInput.MouseButtons.BUTTON_LEFT))
            {
                arma.dispara(ElapsedTime, this.Position, Rotacion);
            }

            this.arma.updateBullets(ElapsedTime);

            var collider = getColliderAABB(obstaculos);
			if (collider != null)
			{
				//esqueleto.Position = lastPos;

				var movementRay = lastPos - Position;

				var rs = Vector3.Empty;
				if (((esqueleto.BoundingBox.PMax.X > collider.PMax.X && movementRay.X > 0) ||
					(esqueleto.BoundingBox.PMin.X < collider.PMin.X && movementRay.X < 0)) &&
					((esqueleto.BoundingBox.PMax.Z > collider.PMax.Z && movementRay.Z > 0) ||
					(esqueleto.BoundingBox.PMin.Z < collider.PMin.Z && movementRay.Z < 0)))
				{
					//Este primero es un caso particularse dan las dos condiciones simultaneamente entonces para saber de que lado moverse hay que hacer algunos calculos mas.
					//por el momento solo se esta verificando que la posicion actual este dentro de un bounding para moverlo en ese plano.
					if (esqueleto.Position.X > collider.PMin.X &&
						esqueleto.Position.X < collider.PMax.X)
					{
						//El personaje esta contenido en el bounding X
						rs = new Vector3(movementRay.X, movementRay.Y, 0);
					}
					if (esqueleto.Position.Z > collider.PMin.Z &&
						esqueleto.Position.Z < collider.PMax.Z)
					{
						//El personaje esta contenido en el bounding Z
						rs = new Vector3(0, movementRay.Y, movementRay.Z);
					}

					//Seria ideal sacar el punto mas proximo al bounding que colisiona y chequear con eso, en ves que con la posicion.

				}
				else
				{
					if ((esqueleto.BoundingBox.PMax.X > collider.PMax.X && movementRay.X > 0) ||
						(esqueleto.BoundingBox.PMin.X < collider.PMin.X && movementRay.X < 0))
					{
						rs = new Vector3(0, movementRay.Y, movementRay.Z);
					}
					if ((esqueleto.BoundingBox.PMax.Z > collider.PMax.Z && movementRay.Z > 0) ||
						(esqueleto.BoundingBox.PMin.Z < collider.PMin.Z && movementRay.Z < 0))
					{
						rs = new Vector3(movementRay.X, movementRay.Y, 0);
					}
				}
				esqueleto.Position = lastPos - rs;

			}

			lastPos = esqueleto.Position;
        }

        public void rotateY(float angle)
        {
            esqueleto.rotateY(angle);
        }


		// Devuelve el bounding box del objeto con el cual esta colisionando
		private TgcBoundingAxisAlignBox getColliderAABB(List<TgcBoundingAxisAlignBox> obstaculos)
		{
			foreach (var obstaculo in obstaculos)
			{
				if (TgcCollisionUtils.testAABBAABB(esqueleto.BoundingBox, obstaculo))
				{
					return obstaculo;
				}
			}
			return null;		}




        //GETTERS Y SETTERS
        public Arma Arma
        {
            get { return arma; }
        }

        public int Health
        {
            get { return health; }
        }
    }
}