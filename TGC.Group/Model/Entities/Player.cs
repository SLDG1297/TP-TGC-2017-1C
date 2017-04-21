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
using Microsoft.DirectX;
using TGC.Core.Utils;

namespace TGC.Group.Model.Entities
{
    public class Player : Personaje
    {
        private bool moving;
        private bool rotating;
        private bool running;
        private bool crouching;

        public Player(string mediaDir, Vector3 initPosition) : base(mediaDir, "CS_Gign", initPosition)
        {
            velocidadCaminar = 250f;
            velocidadIzqDer = 250f;
            velocidadRotacion = 120f;
            tiempoSalto = 10f;
            velocidadSalto = 0.5f;
            resetBooleans();
        }


        private void resetBooleans()
        {

            moving = false;
            rotating = false;
            running = false;
            crouching = false;
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

        public void mover(TgcD3dInput Input, float ElapsedTime){
            //Calcular proxima posicion de personaje segun Input
            var moveForward = 0f;
            var moveLeftRight = 0f;

            float jump = 0;
            var jumpingElapsedTime = 0f;
            float rotate = 0;

            resetBooleans();

            //Correr
            if (running = Input.keyDown(Key.LeftShift))
            {
                setVelocidad(350f, 350f);
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
                    setVelocidad(250f, 250f);
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
                moveForward = velocidadCaminar;
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

            displayAnimations();

            //Actualizar salto
            if (jumping)
            {
                personaje.playAnimation("Jump", true);
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

            personaje.move(moveLeftRight * ElapsedTime, jump, moveForward * ElapsedTime);

        }


        private void displayAnimations()
        {
            if (moving){

                if (running){
                    personaje.playAnimation("Run", true);
                }
                else{
                    if (crouching){
                        personaje.playAnimation("CrouchWalk", true);
                    }
                    else{
                        personaje.playAnimation("Walk", true);
                    }
                }
            }
            else
            {
                if (crouching)
                {
                    personaje.stopAnimation();
                    personaje.playAnimation("CrouchWalk", false);
                    
                }
                else
                {
                    personaje.playAnimation("StandBy", true);
                }
            }
        }

        public void rotateY(float angle)
        {
            personaje.rotateY(angle);
        }

    }
}