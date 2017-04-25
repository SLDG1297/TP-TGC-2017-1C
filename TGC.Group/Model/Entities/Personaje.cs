using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.Geometry;
using TGC.Core.SkeletalAnimation;
using TGC.Core.Utils;

namespace TGC.Group.Model.Entities
{
    public abstract class Personaje
    {
        protected int maxHealth;
        protected int health;
        protected bool muerto;
        protected bool jumping;

        protected TgcSkeletalMesh esqueleto;
        protected Arma arma;

        protected float velocidadCaminar;
        protected float velocidadIzqDer;
        protected float velocidadRotacion;
        protected float tiempoSalto;
        protected float velocidadSalto;

        protected bool moving;
        protected bool rotating;
        protected bool running;
        protected bool crouching;

        //CONSTRUCTORES
        public Personaje(string mediaDir, string skin, Vector3 initPosition)
        {
            muerto = false;

            maxHealth = 100;
            health = maxHealth;
            loadSkeleton(mediaDir, skin);


            esqueleto.AutoTransformEnable = false;
            esqueleto.Position = initPosition;
            esqueleto.Transform = Matrix.Translation(esqueleto.Position);

            resetBooleans();
        }

        public Personaje(string mediaDir, string skin, Vector3 initPosition, Arma arma) :this(mediaDir, skin, initPosition)
        {
            setArma(arma);         
        }

        //METODOS

        //pongo virtual por si otro personaje requiera otras animaciones distintas, entonces cuando lo implementemos
        //solo tenemos que poner 'public override void loadPerson()'
        // skins: CS_Gign, CS_Arctic
        public virtual void loadSkeleton(string MediaDir, string skin)
        {
            //direccion del mesh
            var meshPath = MediaDir + "SkeletalAnimations\\BasicHuman\\" + skin + "-TgcSkeletalMesh.xml";
            //direccion para las texturas
            var mediaPath = MediaDir + "SkeletalAnimations\\BasicHuman\\";

            var skeletalLoader = new TgcSkeletalLoader();

            string[] animationList = {  "StandBy", "Walk", "Jump", "Run", "CrouchWalk" };

            var animationsPath = new string[animationList.Length];
            for (var i = 0; i < animationList.Length; i++)
            {
                //direccion de cada animacion
                animationsPath[i] = MediaDir + "SkeletalAnimations\\BasicHuman\\Animations\\" + animationList[i] + "-TgcSkeletalAnim.xml";
            }

            esqueleto = skeletalLoader.loadMeshAndAnimationsFromFile(meshPath,mediaPath, animationsPath );

            //Configurar animacion inicial    
            esqueleto.playAnimation("StandBy", true);
        }        

        public void recibiDanio(int danio)
        {
            if (danio >= health){
                health = 0;
                muerto = true;
            }
            else{
                health -= danio;
            }
        }

        protected void displayAnimations()
        {
            if (moving)
            {

                if (running)
                {
                    esqueleto.playAnimation("Run", true);
                }
                else
                {
                    if (crouching)
                    {
                        esqueleto.playAnimation("CrouchWalk", true);
                    }
                    else
                    {
                        esqueleto.playAnimation("Walk", true);
                    }
                }
            }
            else
            {
                if (crouching)
                {
                    esqueleto.stopAnimation();
                    esqueleto.playAnimation("CrouchWalk", false);

                }
                else
                {
                    esqueleto.playAnimation("StandBy", true);
                }
            }
        }
        
        public void render(float elapsedTime)
        {
            esqueleto.animateAndRender(elapsedTime);
            arma.renderBullets();
        }

        public void dispose()
        {
            esqueleto.dispose();
        }

        protected void setVelocidad(float caminar, float izqDer)
        {
            velocidadCaminar = caminar;
            velocidadIzqDer = izqDer;
        }
        
        //GETTERS Y SETTERS
        public TgcSkeletalMesh Esqueleto
        {
            get { return esqueleto; }
        }

        public void setArma(Arma arma)
        {
            if (this.arma != null)
            {
                this.arma.dispose();
            }
            this.arma = arma;
            arma.setPlayer(this);
        }

        public Vector3 Position
        {
            get { return esqueleto.Position; }
        }

        protected void resetBooleans()
        {
            moving = false;
            rotating = false;
            running = false;
            crouching = false;
        }

    }
}
