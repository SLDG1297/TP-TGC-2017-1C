using System;
using System.Drawing;

using Microsoft.DirectX;
using Microsoft.DirectX.DirectInput;

using TGC.Core.Camara;
using TGC.Core.Input;
using TGC.Core.Direct3D;
using TGC.Core.Geometry;
using TGC.Core.Textures;
using TGC.Core.Text;
using TGC.Core.Utils;

using TGC.Group.Model.Entities;

namespace TGC.Group.Model
{
	public class Menu
	{
		private string MediaDir;
		private Size windowSize;

		private float refreshTime;
		private int textureIndex;
		private TgcTexture[] textures;
		private TgcPlane background;

		private TgcText2D optionJugar;
		private TgcText2D optionSalir;
        private TgcText2D optionSpectate;

        private Player player;        

        public bool GameStarted { get; private set; }
        public bool FPScamera { get; private set; }

        public Menu(string MediaDir, Size windowSize)
		{
			this.MediaDir = MediaDir;
			this.windowSize = windowSize;
		}

		public void Init()
		{
			InitTextures();

			var sizeX = windowSize.Width;
			var sizeY = windowSize.Height;

			background = new TgcPlane(new Vector3(-sizeX / 2, -sizeY / 2, 0),
			                          new Vector3(sizeX / 2, sizeY / 2, 0),
			                          TgcPlane.Orientations.XYplane, textures[0]);
			InitOptions();
			InitPlayer();
		}

		private void InitOptions()
		{
			optionJugar = new TgcText2D();
			optionJugar.Text = "JUGAR YA";
			optionJugar.Color = Color.White;
			optionJugar.Position = new Point(windowSize.Width / 4, windowSize.Height / 4);
			optionJugar.Size = new Size(optionJugar.Text.Length * 48, 24);
			optionJugar.Align = TgcText2D.TextAlign.LEFT;

			var font = new System.Drawing.Text.PrivateFontCollection();
			font.AddFontFile(MediaDir + "Fonts\\pdark.ttf");
			optionJugar.changeFont(new Font(font.Families[0], 24, FontStyle.Bold));

            
            optionSpectate = new TgcText2D();
            optionSpectate.Text = "MODO ESPECTADOR";
            optionSpectate.Color = Color.White;
            optionSpectate.Position = new Point(windowSize.Width / 4, windowSize.Height / 4 + 50);
            optionSpectate.Size = new Size(optionSpectate.Text.Length * 48, 24);
            optionSpectate.Align = TgcText2D.TextAlign.LEFT;

            optionSpectate.changeFont(new Font(font.Families[0], 24, FontStyle.Bold));


            optionSalir = new TgcText2D();
            optionSalir.Text = "SALIR";
            optionSalir.Color = Color.White;
            optionSalir.Position = new Point(windowSize.Width / 4, windowSize.Height / 4 + 100);
            optionSalir.Size = new Size(optionJugar.Text.Length * 48, 24);
            optionSalir.Align = TgcText2D.TextAlign.LEFT;
            optionSalir.changeFont(new Font(font.Families[0], 24, FontStyle.Bold));
        }

		private void InitTextures()
		{
			textures = new TgcTexture[48];

			for (int i = 0; i < textures.Length; i++)
			{
				textures[i] = TgcTexture.createTexture(D3DDevice.Instance.Device,
				                                       MediaDir + "Menu\\Background\\background-" + i + ".jpg");
			}
		}

		private void InitPlayer()
		{
			player = new Player(MediaDir, "CS_Gign", new Vector3(0, 0, 0), Arma.AK47(MediaDir));
			player.Esqueleto.Transform =
				      Matrix.Scaling(new Vector3(2.5f, 2.5f, 1))
                      * Matrix.RotationY(FastMath.PI)
                      * Matrix.Translation(new Vector3(-windowSize.Width / 3, -windowSize.Height / 3, 100));
		}

		public void Update(float ElapsedTime, TgcD3dInput Input)
		{
			if (Input.keyPressed(Key.Return))
			{
				GameStarted = true;
			}

			refreshTime += ElapsedTime;

			if (refreshTime > 0.05)
			{
				textureIndex++;
				textureIndex %= textures.Length;
				background.setTexture(textures[textureIndex].Clone());
				refreshTime = 0;
			}

			if (TextCollision(Input, optionJugar))
			{
				optionJugar.Color = Color.Yellow;
				if (Input.buttonPressed(TgcD3dInput.MouseButtons.BUTTON_LEFT))
				{
					GameStarted = true;
                    FPScamera = false;
				}
			}
			else
			{
				optionJugar.Color = Color.White;
			}

			if (TextCollision(Input, optionSalir))
			{
				optionSalir.Color = Color.Yellow;
				if (Input.buttonPressed(TgcD3dInput.MouseButtons.BUTTON_LEFT))
				{
					System.Windows.Forms.Application.Exit();
				}
			}
			else
			{
				optionSalir.Color = Color.White;
			}


            if (TextCollision(Input, optionSpectate))
            {
                optionSpectate.Color = Color.Yellow;
                if (Input.buttonPressed(TgcD3dInput.MouseButtons.BUTTON_LEFT))
                {
                    GameStarted = true;
                    FPScamera = true;
                }
            }
            else
            {
                optionSpectate.Color = Color.White;
            }
        }

		private bool TextCollision(TgcD3dInput Input, TgcText2D Text)
		{
			var PminX = Text.Position.X;
			var PmaxX = Text.Position.X + Text.Size.Width;

			var PminY = Text.Position.Y;
			var PmaxY = Text.Position.Y + Text.Size.Height;
			if (PminX <= Input.Xpos && Input.Xpos <= PmaxX
				&& PminY <= Input.Ypos && Input.Ypos <= PmaxY)
			{
				return true;
			}

			return false;
		}

		public void Render()
		{
			background.render();
			optionJugar.render();
			optionSalir.render();
            optionSpectate.render();
			player.render(0);
		}

		public void Dispose()
		{
			foreach (var texture in textures)
			{
				texture.dispose();
			}

			background.dispose();
			player.dispose();
			optionJugar.Dispose();
			optionSalir.Dispose();
            optionSpectate.Dispose();
		}
	}
}
