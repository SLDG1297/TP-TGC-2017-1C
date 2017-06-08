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
using TGC.Group.Model.Collisions;

namespace TGC.Group.Model.Entities
{
    public class Player : Personaje
    {
        
		private float Rotacion = 0f;
        private float jumpingElapsedTime = 0f;

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
            tiempoSalto = 0.6f;
            velocidadSalto = 10f;
            resetBooleans();			
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

		public void mover(TgcD3dInput Input, float posicionY, float ElapsedTime) {
            //Calcular proxima posicion de personaje segun Input
            var moveForward = 0f;
            var moveLeftRight = 0f;

            float jump = 0;
			//float jumpingElapsedTime = 0f;
            float rotate = 0;

            resetBooleans();

			// Rotar respecto a la posicion del mouse
			Rotacion += Input.XposRelative * 0.05f;

            //Correr
            if (running = Input.keyDown(Key.LeftShift)){

                setVelocidad(300f,300f); 
                //es para que exploremos mas rapido el terreno
                //setVelocidad(1700f, 1700f);

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
                    jumpingElapsedTime = 0f;
                }
                else
                {
                    jump = velocidadSalto * (tiempoSalto - jumpingElapsedTime);
                }
            }

            //Disparar
            if (Input.buttonPressed(TgcD3dInput.MouseButtons.BUTTON_LEFT) || Input.buttonDown(TgcD3dInput.MouseButtons.BUTTON_LEFT))
            {
                arma.dispara(ElapsedTime, this.Position, Rotacion);
            }

            esqueleto.rotateY(rotate);
            var desplazamiento = new Vector3(moveLeftRight * ElapsedTime,
                                                jump,// + posicionY - lastPos.Y,
                                                moveForward * ElapsedTime);
            desplazamiento.TransformCoordinate(Matrix.RotationY(Rotacion));
            // esqueleto.Position += desplazamiento;
            var realmove = CollisionManager.Instance.adjustPosition(this, desplazamiento);
            esqueleto.Position += realmove;
            updateBoundingBoxes();

            if(!jumping) CollisionManager.Instance.applyGravity(ElapsedTime);

            //adjustPosition
            //CollisionManager.Instance.adjustPosition(this);
            lastPos = esqueleto.Position;

            esqueleto.Transform = Matrix.RotationY(Utils.DegreeToRadian(rotate))
                                * Matrix.RotationY(Rotacion)
                                * Matrix.Translation(esqueleto.Position);

        }

		protected List<TgcBoundingAxisAlignBox> getColliderAABBList(List<TgcBoundingAxisAlignBox> obstaculos)
		{
			return obstaculos.FindAll(
				delegate (TgcBoundingAxisAlignBox AABB) {
					return TgcCollisionUtils.testAABBAABB(esqueleto.BoundingBox, AABB);
				}
			);
		}

        public void rotateY(float angle)
        {
            esqueleto.rotateY(angle);
        }

		//GETTERS Y SETTERS
        public Arma Arma
        {
            get { return arma; }
        }

        public bool Jumping{
            get { return jumping; }
        }


        public void adjustYPos(float y)
        {
            esqueleto.Position= new Vector3(esqueleto.Position.X, y ,esqueleto.Position.Z);
        }
    }
}