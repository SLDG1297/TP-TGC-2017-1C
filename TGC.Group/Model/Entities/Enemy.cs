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
        /// <summary>
        ///     Construye un enemigo que trata de encontrar al jugador y matarlo.
        /// </summary>
        /// <param name="mediaDir">Ruta donde esta la carpeta con los assets</param>
        /// <param name="skin">Nombre del skin a usar (ej: CS_Gign, CS_Arctic)</param>
        /// <param name="skin">Posicion inicial del jugador</param>
        /// <param name="arma">Arma con la que el jugador comienzar</param> 
        public Enemy(string mediaDir, string skin, Vector3 initPosition, Arma arma) :base(mediaDir, skin, initPosition,arma){

            maxHealth = 100;

            velocidadCaminar = 400f;
            velocidadIzqDer = 300f;
            velocidadRotacion = 120f;
            tiempoSalto = 10f;
            velocidadSalto = 0.5f;

        }

    }
}
