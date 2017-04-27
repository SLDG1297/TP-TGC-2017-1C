using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.SkeletalAnimation;
using TGC.Core.BoundingVolumes;

namespace TGC.Group.Model.Entities
{
	public class Enemy : Personaje
	{
		// Setear estatus de la IA. 0 = Parado, 1 = escapando, 2 = persiguiendo al jugador
		private int estatus_ia;
		private float Rotacion = 0f;
		private Vector3 lastPos; // Ultima posicion
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
				float dot = Vector3.Dot(direccion, -dir_movimiento);
				if (dot > Math.PI / 3) { 
					rotate = (float)Math.Acos(System.Convert.ToDouble(dot));
				}
			}

			if (estatus_ia == 2) {
				moving = true;
				dir_movimiento = posicionJugador - Position;
				moveForward = -velocidadCaminar;
				float dot = Vector3.Dot(direccion, dir_movimiento);
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

			esqueleto.Position += desplazamiento;
			esqueleto.Transform = Matrix.RotationY(rotate) * Matrix.Translation(esqueleto.Position);

			var collider = getColliderAABB(obstaculos);
			if (collider != null)
			{
				//esqueleto.Position = lastPos;

				var movementRay = lastPos - Position;

				var rs = Vector3.Empty;
				if (((esqueleto.BoundingBox.PMax.X > collider.PMax.X && movementRay.X > 0) ||
					(esqueleto.BoundingBox.PMin.X < collider.PMin.X && movementRay.X < 0)) &&
					((esqueleto.BoundingBox.PMax.Z > collider.PMax.Z && movementRay.Z > 0) ||
					(esqueleto.BoundingBox.PMin.Z < collider.PMin.Z && movementRay.Z < 0)))
				{
					//Este primero es un caso particularse dan las dos condiciones simultaneamente entonces para saber de que lado moverse hay que hacer algunos calculos mas.
					//por el momento solo se esta verificando que la posicion actual este dentro de un bounding para moverlo en ese plano.
					if (esqueleto.Position.X > collider.PMin.X &&
						esqueleto.Position.X < collider.PMax.X)
					{
						//El personaje esta contenido en el bounding X
						rs = new Vector3(movementRay.X, movementRay.Y, 0);
					}
					if (esqueleto.Position.Z > collider.PMin.Z &&
						esqueleto.Position.Z < collider.PMax.Z)
					{
						//El personaje esta contenido en el bounding Z
						rs = new Vector3(0, movementRay.Y, movementRay.Z);
					}

					//Seria ideal sacar el punto mas proximo al bounding que colisiona y chequear con eso, en ves que con la posicion.

				}
				else
				{
					if ((esqueleto.BoundingBox.PMax.X > collider.PMax.X && movementRay.X > 0) ||
						(esqueleto.BoundingBox.PMin.X < collider.PMin.X && movementRay.X < 0))
					{
						rs = new Vector3(0, movementRay.Y, movementRay.Z);
					}
					if ((esqueleto.BoundingBox.PMax.Z > collider.PMax.Z && movementRay.Z > 0) ||
						(esqueleto.BoundingBox.PMin.Z < collider.PMin.Z && movementRay.Z < 0))
					{
						rs = new Vector3(movementRay.X, movementRay.Y, 0);
					}
				}

				esqueleto.Position = 
				esqueleto.Position = lastPos - rs;

			}

			lastPos = esqueleto.Position;
			lastPosTerreno = posicionY;

		}
	}
}
