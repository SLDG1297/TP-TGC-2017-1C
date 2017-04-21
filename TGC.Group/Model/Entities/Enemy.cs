using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.SkeletalAnimation;

namespace TGC.Group.Model.Entities
{
    public class Enemy : Personaje
    {

        public Enemy(string mediaDir, Vector3 initPosition) : base(mediaDir, "CS_Arctic", initPosition)
        {
            maxHealth = 100;

            velocidadCaminar = 400f;
            velocidadIzqDer = 300f;
            velocidadRotacion = 120f;
            tiempoSalto = 10f;
            velocidadSalto = 0.5f;
        }

        public Enemy(string mediaDir, string skin, Vector3 initPosition) : base(mediaDir, skin, initPosition)
        {
            maxHealth = 100;

            velocidadCaminar = 400f;
            velocidadIzqDer = 300f;
            velocidadRotacion = 120f;
            tiempoSalto = 10f;
            velocidadSalto = 0.5f;
        }
    }
}
