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
        public Player(string mediaDir, Vector3 initPosition) : base(mediaDir, "CS_Gign", initPosition)
        {
            velocidadCaminar = 400f;
            velocidadIzqDer = 300f;
            velocidadRotacion = 120f;
            tiempoSalto = 10f;
            velocidadSalto = 0.5f;
        }

        public void recuperaSalud(int salud)
        {
            if (salud + health > maxHealth)
            {
                health = maxHealth;
            }
            else
            {
                health += salud;
            }
        }

        public void mover(TgcD3dInput Input, float ElapsedTime)
        {
            //Calcular proxima posicion de personaje segun Input
            var moveForward = 0f;
            var moveLeftRight = 0f;

            float jump = 0;
            var jumpingElapsedTime = 0f;
            float rotate = 0;

            var moving = false;
            var rotating = false;
            var running = false;

            //Correr
            if (running = Input.keyDown(Key.LeftShift))
            {
                velocidadCaminar = 450f;
            }
            else
            {
                velocidadCaminar = 400f;
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

            if (moving)
            {
                if (running)
                {//Activar animacion de caminando
                    personaje.playAnimation("Run", true);
                }
                else
                {
                    personaje.playAnimation("Walk", true);

                }
                //Aplicar movimiento hacia adelante o atras segun la orientacion actual del Mesh
                var lastPos = personaje.Position;
            }
            else
            { //Si no se esta moviendo, activar animacion de Parado
                if (muerto) personaje.playAnimation("CrouchWalk", true);
                else
                {
                    personaje.playAnimation("StandBy", true);
                }
            }

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

        public void rotateY(float angle)
        {
            personaje.rotateY(angle);
        }

    }
}