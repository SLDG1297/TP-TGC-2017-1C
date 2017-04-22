using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.SkeletalAnimation;
using TGC.Core.Utils;

namespace TGC.Group.Model.Entities
{
    public class Arma
    {
        private int balas;
        private int recargas;
        private TgcSkeletalBoneAttach attachment;

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

        public void dispara()
        {

        }

        public void recarga()
        {

        }
    }
}
