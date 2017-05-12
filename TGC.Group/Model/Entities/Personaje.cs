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
using TGC.Core.BoundingVolumes;
using TGC.Core.Collision;

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
        protected TgcBoundingCylinderFixedY cylinderBB;
        protected TgcBoundingCylinderFixedY cylinderHead;

        protected Vector3 lastPos; // Ultima posicion

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

            lastPos = initPosition;
            cylinderBB = new TgcBoundingCylinderFixedY(esqueleto.BoundingBox.calculateBoxCenter(), 8, 25);

            var x = esqueleto.BoundingBox.calculateBoxCenter().X;
            var y = esqueleto.BoundingBox.calculateBoxCenter().Y + 20;
            var z = esqueleto.BoundingBox.calculateBoxCenter().Z;

            Vector3 center = new Vector3(x,y,z);

            cylinderHead = new TgcBoundingCylinderFixedY(center, 3, 3);                  
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

		// Devuelve el bounding box del objeto con el cual esta colisionando
		protected TgcBoundingAxisAlignBox getColliderAABB(List<TgcBoundingAxisAlignBox> obstaculos)
		{
			foreach (var obstaculo in obstaculos)
			{
				if (TgcCollisionUtils.testAABBAABB(esqueleto.BoundingBox, obstaculo))
				{
					return obstaculo;
				}
			}
			return null;
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

                
        public void updateBoundingBoxes()
        {
            BoundingCylinder.move(Position - lastPos);
            cylinderHead.move(Position - lastPos);

            BoundingCylinder.updateValues();
            cylinderHead.updateValues();
        }


        public void moveCylindersXZ(Vector3 dif)
        {
            cylinderBB.move(dif.X,0,dif.Z);
            cylinderHead.move(dif.X,0,dif.Z);
        }

        public void render(float elapsedTime)
        {
            esqueleto.animateAndRender(elapsedTime);        
        }

        public void dispose()
        {
            arma.dispose();
            //esqueleto.dispose();            
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

        public int Health
        {
            get { return health; }
        }

        public bool Muerto
        {
            get { return muerto; }
        }

        public Vector3 LastPosition
        {
            get { return lastPos; }
        }


        public Vector3 Position
        {
            get { return esqueleto.Position; }
        }

        public TgcBoundingCylinderFixedY BoundingCylinder
        {
            get { return cylinderBB; }
        }

        public TgcBoundingCylinderFixedY HeadCylinder
        {
            get { return cylinderHead; }
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
