using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.BoundingVolumes;
using TGC.Core.SceneLoader;
using TGC.Core.SkeletalAnimation;
using TGC.Core.Utils;
using TGC.Group.Model.Collisions;

namespace TGC.Group.Model.Entities
{
    public class Arma
    {
        private int balas;
        private int recargas;
        private TgcSkeletalBoneAttach attachment;
        private string media;
        private int danioBala;

        //Por cada arma generamos un constructor, asi no tenemos que setear el path a manopla y
        //viene de una
        public static Arma AK47(string mediaDir)
        {
            //instancio el arma, con la direccion del skin hardcodeada
            Arma arma = new Arma(mediaDir, "Meshes\\Armas\\AK47\\AK47-TgcScene.xml");

            //desplazo al arma para que quede al lado de la mano, depende de como venga cada mesh
            arma.attachment.Offset = Matrix.Translation(-25, -3, -65) * Matrix.Scaling(0.5f, 0.5f, 0.5f) * Matrix.RotationX(FastMath.ToRad(90));

            arma.balas = 35;
            arma.recargas = 3;
            arma.danioBala = 15;

            return arma;
        }

        private Arma(string mediaDir, string meshPath)
        {
            var loader = new Core.SceneLoader.TgcSceneLoader();

            attachment = new TgcSkeletalBoneAttach();
            //cargo el mesh de dicha arma
            attachment.Mesh = loader.loadSceneFromFile(mediaDir + meshPath).Meshes[0];
            media = mediaDir;
        }

        public void setPlayer(Personaje personaje)
        {
            //anclo el arma a la manito del chabon
            attachment.Bone = personaje.Esqueleto.getBoneByName("Bip01 R Hand");
            attachment.updateValues();
            attachment.Mesh.Enabled = true;

            //aniado el arma a la lista de attachments del esqueleto
            personaje.Esqueleto.Attachments.Add(attachment);
        }

        public void dispose()
        {
           	attachment.Mesh.dispose();
        }

        //aniado un mesh a la lista de proyectiles para  luego calcular su posicion
        //necesito la posicion de partida para luego moverlo (en este caso, la del jugador)
        public void dispara(float elapsedTime, Vector3 position, float angulo)
        {
            if (balas > 0)
            {
                var bala = new Bala(media, position, angulo,danioBala);
                CollisionManager.Instance.agregarBala(bala);
                balas--;
            }
        }

        public void recarga()
        {
            if (recargas > 0 && balas < 30)
            {
                balas = 30;
                recargas--;
            }
        }
        
        //GETTERS Y SETTERS
        public int Balas
        {
            get { return balas; }
        }

        public int Recargas
        {
            get { return recargas; }
        }

        public int DanioBala
        {
            get { return danioBala; }
        }
    }

    public class Bala
    {
        private Vector3 direccion;
        private TgcMesh bala;
        private int danio;
        private bool impacto = false;

        public Bala(string mediaDir, Vector3 pos, float angulo, int danio)
        {
            var loader = new Core.SceneLoader.TgcSceneLoader();
            bala = loader.loadSceneFromFile(mediaDir + "Meshes\\Armas\\Bullet\\Bullet-TgcScene.xml").Meshes[0];

            bala.Position = pos + new Vector3(0, 30, 0);
            bala.Scale = new Vector3(0.005f, 0.005f, 0.005f);
            bala.rotateX(-FastMath.PI_HALF);
            bala.rotateY(-FastMath.ToRad(angulo));

            this.danio = danio;

            direccion = new Vector3(0, 0, -700f);
            direccion.TransformCoordinate(Matrix.RotationY(angulo));
            bala.createBoundingBox();
        }

        public void update(float ElapsedTime)
        {
            var desplazamiento = direccion * ElapsedTime;

            bala.Position += desplazamiento;
            bala.updateBoundingBox();
            bala.AutoTransformEnable = false;
            bala.Transform = Matrix.RotationX(bala.Rotation.X) * Matrix.Scaling(bala.Scale) * Matrix.Translation(bala.Position);
            bala.BoundingBox.transform(bala.Transform);            
        }

        public void dispose()
        {
            bala.dispose();
        }

        public void render()
        {
            if (!impacto)
            {
                bala.render();
                bala.BoundingBox.render();
            }
        }

        //GETTERS Y SETTERS
        public TgcMesh Mesh
        {
            get { return bala; }
        }

        public TgcBoundingAxisAlignBox BoundingBox
        {
            get { return bala.BoundingBox; }
        }

        public int Danio
        {
            get { return danio; }
        }

        public void setImpacto(bool b)
        {
            impacto = b;
        }

        public bool Impacto
        {
            get { return impacto; }
        }
        
    }
}