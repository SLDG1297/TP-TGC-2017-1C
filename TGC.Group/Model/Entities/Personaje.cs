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
            var skeletalLoader = new TgcSkeletalLoader();
            personaje = skeletalLoader.loadMeshAndAnimationsFromFile(

                MediaDir + "SkeletalAnimations\\BasicHuman\\" + skin + "-TgcSkeletalMesh.xml",
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

    }
}
