using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.SceneLoader;
using TGC.Core.SkeletalAnimation;
using TGC.Core.Utils;

namespace TGC.Group.Model.Entities
{
    public class Arma
    {
        private int balas;
        private int recargas;
        private TgcSkeletalBoneAttach attachment;
        private TgcMesh bala;
        private List<TgcMesh> proyectiles = new List<TgcMesh>();


        //Por cada arma generamos un constructor, asi no tenemos que setear el path a manopla y
        //viene de una
        public static Arma AK47(string mediaDir)
        {
            //instancio el arma, con la direccion del skin hardcodeada
            Arma arma = new Arma(mediaDir, "Meshes\\Armas\\AK47\\AK47-TgcScene.xml");

            //desplazo al arma para que quede al lado de la mano, depende de como venga cada mesh
            arma.attachment.Offset = Matrix.Translation(-25, -3, -65) * Matrix.Scaling(0.5f, 0.5f, 0.5f) * Matrix.RotationX(FastMath.ToRad(90));
            
            arma.balas = 30;
            arma.recargas = 3;

            return arma;
        }

        private Arma(string mediaDir, string meshPath)
        {
            var loader = new Core.SceneLoader.TgcSceneLoader();
            bala = loader.loadSceneFromFile(mediaDir + "Meshes\\Armas\\Bullet\\Bullet-TgcScene.xml").Meshes[0];

            attachment = new TgcSkeletalBoneAttach();
            //cargo el mesh de dicha arma
            attachment.Mesh = loader.loadSceneFromFile(mediaDir + meshPath).Meshes[0]; 
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
        public void dispara(float elapsedTime, Vector3 position)
        {
            if (balas > 0)
            {
                var instance = bala.createMeshInstance(bala.Name + proyectiles.Count + 1);

                instance.AutoTransformEnable = false;
                var desplazamiento = new Vector3(0, 40, 0);
                instance.Position = position + desplazamiento;
                instance.Transform = Matrix.Translation(instance.Position);
                proyectiles.Add(instance);

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

        //GESTION DE BALAS
        public void renderBullets()
        {
            if (proyectiles.Count != 0) Utils.renderMeshes(proyectiles);
        }

        public void updateBullets(float elapsedTime, float angulo)
        {
            if (proyectiles.Count != 0)
            {
                foreach (var bala in proyectiles)
                {
                    var desplazamiento = new Vector3(0, 0, -700f * elapsedTime);
					desplazamiento.TransformCoordinate(Matrix.RotationY(angulo));

                    bala.AutoTransformEnable = false;
                    bala.Position += desplazamiento;
                    bala.Transform = Matrix.RotationX(-FastMath.PI_HALF) * Matrix.Scaling(0.005f, 0.005f, 0.005f) * Matrix.Translation(bala.Position);
                }
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
    }
}
