using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.SkeletalAnimation;
using TGC.Core.Input;
using Microsoft.DirectX.DirectInput;
using TGC.Core.Collision;
using Microsoft.DirectX;

namespace TGC.Group.Model
{
    class Player
    {
        private int maxHealth;
        private int health;
        private bool muerto;
        private TgcSkeletalMesh personaje;

        public Player(string mediaDir, Vector3 initPosition)
        {
            maxHealth = 100;
            muerto = false;
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
                    MediaDir + "SkeletalAnimations\\BasicHuman\\Animations\\Walk-TgcSkeletalAnim.xml"
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

        public Vector3 Position(){
            return personaje.Position;
        }

        public void mover(TgcD3dInput Input, float ElapsedTime)
        {
            var velocidadCaminar = 400f;
            var velocidadRotacion = 120f;
            //Calcular proxima posicion de personaje segun Input
            var moveForward = 0f;
            float rotate = 0;
            var moving = false;
            var rotating = false;

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
                rotate = velocidadRotacion;
                rotating = true;
            }

            //Izquierda
            if (Input.keyDown(Key.A))
            {
                rotate = -velocidadRotacion;
                rotating = true;
            }


            if (moving)
            {
                //Activar animacion de caminando
                personaje.playAnimation("Walk", true);

                //Aplicar movimiento hacia adelante o atras segun la orientacion actual del Mesh
                var lastPos = personaje.Position;

                //La velocidad de movimiento tiene que multiplicarse por el elapsedTime para hacerse independiente de la velocida de CPU
                //Ver Unidad 2: Ciclo acoplado vs ciclo desacoplado
                personaje.moveOrientedY(moveForward * ElapsedTime);


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