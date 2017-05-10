using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.SkeletalAnimation;
using TGC.Core.BoundingVolumes;
using TGC.Group.Model.Collisions;

namespace TGC.Group.Model.Entities
{
	public class Enemy : Personaje
	{
		// Setear estatus de la IA. 0 = Parado, 1 = escapando, 2 = persiguiendo al jugador
		private int estatus_ia;
		private float Rotacion = 0f;
		private float lastPosTerreno;


		/// <summary>
		///     Construye un enemigo que trata de encontrar al jugador y matarlo.
		/// </summary>
		/// <param name="mediaDir">Ruta donde esta la carpeta con los assets</param>
		/// <param name="skin">Nombre del skin a usar (ej: CS_Gign, CS_Arctic)</param>
		/// <param name="skin">Posicion inicial del jugador</param>
		/// <param name="arma">Arma con la que el jugador comienzar</param> 
		public Enemy(string mediaDir, string skin, Vector3 initPosition, Arma arma) : base(mediaDir, skin, initPosition, arma)
		{

			maxHealth = 100;

			velocidadCaminar = 50f;
			velocidadIzqDer = 50f;
			velocidadRotacion = 120f;
			tiempoSalto = 10f;
			velocidadSalto = 0.5f;

			estatus_ia = 0;
		}

		public bool isCollidingWithObject(List<TgcBoundingAxisAlignBox> obstaculos) { 
			 var collider = getColliderAABB(obstaculos);
			return collider != null;
		}

		public void updateStatus(Vector3 posicionJugador, float elapsedTime, List<TgcBoundingAxisAlignBox> obstaculos, float posicionY)
		{
			var dir_escape = this.Position - posicionJugador;
			dir_escape.Y = 0;
			var dist = dir_escape.Length();
			System.Console.Out.WriteLine(dist);

			switch (estatus_ia)
			{
				case 0:
					if (dist <= 250) { 
						estatus_ia = 1;
					}
					break;

				case 1:
					if (dist >= 400) { 
						estatus_ia = 0;
					}
					break;

				case 2:
					if (dist < 400) { 
						estatus_ia = 0;
					}
					break;
			}
			mover(posicionJugador, obstaculos, elapsedTime, posicionY);
		}

		public void mover(Vector3 posicionJugador, List<TgcBoundingAxisAlignBox> obstaculos, float elapsedTime, float posicionY)
		{
			var moveForward = 0f;
			var direccion = Position - new Vector3(Position.X, Position.Y, Position.Z + 1);
			float rotate = 0;
			Vector3 dir_movimiento = new Vector3(0,0,0);

			resetBooleans();

			if (estatus_ia == 1){
				moving = true;
				dir_movimiento = Position - posicionJugador;
				moveForward = velocidadCaminar - 10f;
				float dot = Math.Abs(Vector3.Dot(direccion, -dir_movimiento));
				if (dot > Math.PI / 3) { 
					rotate = (float)Math.Acos(System.Convert.ToDouble(dot));
				}
			}

			if (estatus_ia == 2) {
				moving = true;
				dir_movimiento = posicionJugador - Position;
				moveForward = -velocidadCaminar;
				float dot = Math.Abs(Vector3.Dot(direccion, dir_movimiento));
				if (dot > Math.PI / 3)
				{
					rotate = (float)Math.Acos(System.Convert.ToDouble(dot));
				}
			}

			if (estatus_ia == 0) {
				moving = false;
				moveForward = 0;
				rotate = 0;

			}

			displayAnimations();

			dir_movimiento.Y = 0;

			var desplazamiento = new Vector3(0, 0, 0);
			if (Vector3.Length(dir_movimiento) > 0){
				desplazamiento = Vector3.Scale(dir_movimiento, 1 / Vector3.Length(dir_movimiento));
				desplazamiento = Vector3.Scale(desplazamiento, moveForward);
			}

			desplazamiento.Y = posicionY - esqueleto.Position.Y;
            desplazamiento.TransformCoordinate(Matrix.RotationY(esqueleto.Rotation.Y));
            esqueleto.Position += desplazamiento;

            updateBoundingBoxes();

            esqueleto.Transform = Matrix.Translation(esqueleto.Position);
            CollisionManager.Instance.adjustPosition(this);

                        
			lastPos = esqueleto.Position;
			lastPosTerreno = posicionY;

		}
	}
}
