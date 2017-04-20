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

namespace TGC.Group.Model
{
    public class Player
    {
        private float velocidadCaminar = 400f;
        private float velocidadIzqDer = 300f;
        private float velocidadRotacion = 120f;
        private float tiempoSalto = 10f;
        private float velocidadSalto = 0.5f;

        private int maxHealth = 100;
        private int health;
        private bool muerto;
        private bool jumping;
        private TgcSkeletalMesh personaje;

        public Player(string mediaDir, Vector3 initPosition)
        {
            muerto = false;
            health = maxHealth;
            loadPerson(mediaDir);
            personaje.move(initPosition);
        }

        public void loadPerson(string MediaDir)
        {
            var skeletalLoader = new TgcSkeletalLoader();
            personaje = skeletalLoader.loadMeshAndAnimationsFromFile(

                //TODO: Podríamos tener un skin para el jugador principal, y el de los enemigos los terro, re counter jaja

                //MediaDir + "SkeletalAnimations\\BasicHuman\\CS_Arctic-TgcSkeletalMesh.xml",
                //MediaDir + "SkeletalAnimations\\BasicHuman\\CombineSoldier-TgcSkeletalMesh.xml",
                //MediaDir + "SkeletalAnimations\\BasicHuman\\Pilot-TgcSkeletalMesh.xml",

                MediaDir + "SkeletalAnimations\\BasicHuman\\CS_Gign-TgcSkeletalMesh.xml",
                MediaDir + "SkeletalAnimations\\BasicHuman\\",
                new[]
                {
                    MediaDir + "SkeletalAnimations\\BasicHuman\\Animations\\StandBy-TgcSkeletalAnim.xml",
                    MediaDir + "SkeletalAnimations\\BasicHuman\\Animations\\Walk-TgcSkeletalAnim.xml",
                    MediaDir + "SkeletalAnimations\\BasicHuman\\Animations\\Jump-TgcSkeletalAnim.xml",
                    MediaDir + "SkeletalAnimations\\BasicHuman\\Animations\\Run-TgcSkeletalAnim.xml"
                });


            //Configurar animacion inicial
            personaje.playAnimation("StandBy", true);
        }

        public void recibiDanio(int danio){
            if (danio >= health){
                health = 0;
                muerto = true;                
            }
            else{
                health -= danio;
            }
        }

        public void recuperaSalud(int salud){
            if(salud + health > maxHealth){
                health = maxHealth;
            }
            else{
                health += salud;
            }
        }

        public Vector3 Position
        {
            get { return personaje.Position; }
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
            if (Input.keyDown(Key.LeftShift)) {
                velocidadCaminar = 450f;
                running = true;
            }
            else{
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
                moveLeftRight = - velocidadIzqDer;
                rotate = velocidadRotacion;
                rotating = true;
                moving = true;
            }

            //Izquierda
            if (Input.keyDown(Key.A))
            {
                moveLeftRight =velocidadIzqDer;
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
                //Activar animacion de caminando
                personaje.playAnimation("Walk", true);
                if (running)
                {
                    personaje.stopAnimation();
                    personaje.playAnimation("Run", true);
                }

                //Aplicar movimiento hacia adelante o atras segun la orientacion actual del Mesh
                var lastPos = personaje.Position;               
            }
            //Si no se esta moviendo, activar animacion de Parado
            else
            {
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

        public void render(float elapsedTime)
        {
            personaje.Transform = Matrix.Translation(personaje.Position);
            personaje.animateAndRender(elapsedTime);
        }

        public void dispose(){
            personaje.dispose();
        }
    }
}