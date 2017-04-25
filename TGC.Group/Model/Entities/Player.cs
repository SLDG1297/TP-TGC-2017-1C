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
            var jumpingElapsedTime = 0f;
            float rotate = 0;

            resetBooleans();

            //Correr
            if (running = Input.keyDown(Key.LeftShift)){
                setVelocidad(200f, 200f);
            }
            else
            {
                //Agacharse
                if (crouching = Input.keyDown(Key.LeftControl))
                {
                    setVelocidad(30f, 30f);
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
                moveForward = velocidadCaminar - 50f;
                moving = true;
            }

            //Derecha
            if (Input.keyDown(Key.D))
            {
                moveLeftRight = -velocidadIzqDer;
                rotate = velocidadRotacion;
                rotating = true;
                moving = true;
            }

            //Izquierda
            if (Input.keyDown(Key.A))
            {
                moveLeftRight = velocidadIzqDer;
                rotate = -velocidadRotacion;
                rotating = true;
                moving = true;
            }

            //Saltar
            if (!jumping && Input.keyPressed(Key.Space))
            {
                jumping = true;
            }

            //Disparar
            if (Input.buttonPressed(TgcD3dInput.MouseButtons.BUTTON_LEFT) || Input.buttonDown(TgcD3dInput.MouseButtons.BUTTON_LEFT))
            {
                arma.dispara(ElapsedTime, this.Position);
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
			esqueleto.Position += desplazamiento;
            esqueleto.Transform = Matrix.Translation(esqueleto.Position);
            this.arma.updateBullets(ElapsedTime);


            var collider = getColliderAABB(obstaculos);
			if (collider != null)
			{
				// TODO: Si hay colision, hacer algo
				esqueleto.Position = lastPos;
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