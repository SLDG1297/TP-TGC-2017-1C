using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.SkeletalAnimation;

namespace TGC.Group.Model.Entities
{
    public abstract class Personaje
    {
        protected int maxHealth;
        protected int health;
        protected bool muerto;
        protected bool jumping;
        protected TgcSkeletalMesh personaje;

        protected float velocidadCaminar;
        protected float velocidadIzqDer;
        protected float velocidadRotacion;
        protected float tiempoSalto;
        protected float velocidadSalto;

        public Personaje(string mediaDir, string skin, Vector3 initPosition)
        {
            muerto = false;
            health = maxHealth;
            loadPerson(mediaDir, skin);
            personaje.move(initPosition);
        }

        //pongo virtual por si otro personaje requiera otras animaciones distintas, entonces cuando lo implementemos
        //solo tenemos que poner 'public override void loadPerson()'
        public virtual void loadPerson(string MediaDir, string skin)
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

            personaje = skeletalLoader.loadMeshAndAnimationsFromFile(meshPath,mediaPath, animationsPath );
            //Configurar animacion inicial
            personaje.playAnimation("StandBy", true);
        }

        public void recibiDanio(int danio)
        {
            if (danio >= health)
            {
                health = 0;
                muerto = true;
            }
            else
            {
                health -= danio;
            }
        }


        public Vector3 Position
        {
            get { return personaje.Position; }
        }

        public void render(float elapsedTime)
        {
            personaje.Transform = Matrix.Translation(personaje.Position);
            personaje.animateAndRender(elapsedTime);
        }

        public void dispose()
        {
            personaje.dispose();
        }

        protected void setVelocidad(float caminar, float izqDer)
        {
            velocidadCaminar = caminar;
            velocidadIzqDer = izqDer;
        }
    }
}
