using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.Direct3D;
using TGC.Core.Text;
using TGC.Core.Utils;
using TGC.Group.Model.Entities;

namespace TGC.Group.Model.UI
{
    public class UIManager
    {
        private Drawer2D drawer2D;
        private CustomSprite healthIcon;
        private CustomSprite ammoIcon;
        private CustomSprite healthBar;
        private CustomSprite healthBarBorder;
        private CustomSprite reloadsIcon;
        private CustomSprite crosshair;

        // HUD
        private TgcText2D textoAmmo = new TgcText2D();
        private TgcText2D textoHealth = new TgcText2D();
        private TgcText2D textoRecargas = new TgcText2D();

        private TgcText2D sombraTexto = new TgcText2D();
        private bool healthBarEnabled = true;

        public void Init(string MediaDir)
        {
            initHUD(MediaDir);
            initText(MediaDir);
        }

        public void Update(Player player, float ElapsedTime)
        {
            updateHealthBar(player);
            updateText(player);
        }

        public void Render()
        {   
            //Iniciar dibujado de todos los Sprites de la escena (en este caso es solo uno)
            drawer2D.BeginDrawSprite();

            //Dibujar sprite (si hubiese mas, deberian ir todos aquí)
            drawer2D.DrawSprite(ammoIcon);
            drawer2D.DrawSprite(healthIcon);

            if(healthBarEnabled) drawer2D.DrawSprite(healthBar);

            drawer2D.DrawSprite(healthBarBorder);
            drawer2D.DrawSprite(reloadsIcon);
            //drawer2D.DrawSprite(crosshair);
            //Finalizar el dibujado de Sprites
            drawer2D.EndDrawSprite();


            textoAmmo.render();
            textoHealth.render();
            sombraTexto.render();
            textoRecargas.render();
        }

        public void Dispose()
        {
            ammoIcon.Dispose();
            healthIcon.Dispose();
            healthBar.Dispose();
            healthBarBorder.Dispose();
            reloadsIcon.Dispose();
            //crosshair.Dispose();

            textoAmmo.Dispose();
            sombraTexto.Dispose();
            textoHealth.Dispose();
            textoRecargas.Dispose();
        }

        public CustomSprite initSprite(string direccion)
        {
            //Crear Sprite
            CustomSprite sprite = new CustomSprite();
            sprite.Bitmap = new CustomBitmap(direccion, D3DDevice.Instance.Device);

            return sprite;
        }

        private void initHUD(string MediaDir)
        {
            var device = D3DDevice.Instance;
            drawer2D = new Drawer2D();

            //Crear Sprite - Iconito de Balas
            ammoIcon = initSprite(MediaDir + "\\Texturas\\Sprites\\ammunition.png");
            var ammotextureSize = ammoIcon.Bitmap.Size;
            var posicionAlineamiento = ammoIcon.Bitmap.Size.Height / 2;

            ammoIcon.Color = System.Drawing.Color.OrangeRed;
            ammoIcon.Scaling = new Vector2(0.25f, 0.25f);
            ammoIcon.Position = new Vector2((device.Width / 2) + 200,
                                              device.Height - posicionAlineamiento);

            //Crear Sprite - Iconito de Salud
            healthIcon = initSprite(MediaDir + "\\Texturas\\Sprites\\health.png");

            var healtTextureSize = healthIcon.Bitmap.Size;
            healthIcon.Color = System.Drawing.Color.OrangeRed;

            healthIcon.Scaling = new Vector2(0.10f, 0.10f);
            healthIcon.Position = new Vector2((device.Width / 2) - 400,
                                              device.Height - posicionAlineamiento);


            //HealthBar - bordecito
            healthBarBorder = initSprite(MediaDir + "\\Texturas\\Sprites\\healthBarBorder.png");
            healthBarBorder.Scaling = new Vector2(0.75f, 0.5f);
            healthBarBorder.Position = new Vector2((device.Width / 2) - 250,
                                                   device.Height - posicionAlineamiento + 10);

            //HealthBar
            healthBar = initSprite(MediaDir + "\\Texturas\\Sprites\\healthBar.png");
            healthBar.Position = healthBarBorder.Position;

            healthBar.Color = Color.OrangeRed;

            var srcRect = new Rectangle();
            srcRect.Y = healthBar.Bitmap.Size.Width;
            srcRect.Width = healthBar.Bitmap.Size.Width;
            srcRect.X = healthBar.Bitmap.Size.Height;
            srcRect.Height = healthBar.Bitmap.Size.Height;
            healthBar.SrcRect = srcRect;

            healthBar.Scaling = new Vector2(0.75f, 0.22f);

            //Crear Sprite - Iconito de Recargas
            reloadsIcon = initSprite(MediaDir + "\\Texturas\\Sprites\\reloads.png");
            reloadsIcon.Color = System.Drawing.Color.OrangeRed;

            reloadsIcon.Scaling = new Vector2(0.03f, 0.035f);
            reloadsIcon.Position = new Vector2((device.Width / 2) + 400 ,
                                                device.Height - posicionAlineamiento);

            //Crear Sprite - Iconito de mira
            //Por ahora no lo puedo ubicar en el centro exactamente
            /*
            crosshair = initSprite(MediaDir + "\\Texturas\\Sprites\\crosshair.png");
            var crosshairSize = crosshair.Bitmap.Size;

            crosshair.Color = System.Drawing.Color.OrangeRed;


            crosshair.Scaling = new Vector2(0.05f, 0.05f);
            crosshair.Position = new Vector2(device.Width / 2,
                                             device.Height / 2);
            */
            //crosshair.Position = new Vector2(device.Width / 2, device.Height / 2);

        }


        private void initText(string MediaDir)
        {
            //texto de cuantas balas le quedan al jugador
            textoAmmo.Color = Color.DarkOrange;
            var des = ammoIcon.Position + new Vector2(60, 10);
            textoAmmo.Position = new Point((int)des.X, (int)des.Y);
            textoAmmo.Size = new Size(3 * 24, 24);
            textoAmmo.Align = TgcText2D.TextAlign.LEFT;

            var font = new System.Drawing.Text.PrivateFontCollection();
            font.AddFontFile(MediaDir + "Fonts\\pdark.ttf");
            textoAmmo.changeFont(new System.Drawing.Font(font.Families[0], 24, FontStyle.Bold));


            sombraTexto.Color = Color.DarkGray;
            sombraTexto.Position = new Point((int)des.X + 3, (int)des.Y + 2);
            sombraTexto.Size = new Size(3 * 24, 24);
            sombraTexto.Align = TgcText2D.TextAlign.LEFT;
            sombraTexto.changeFont(new System.Drawing.Font(font.Families[0], 24, FontStyle.Bold));


            //texto de la energia que tiene
            textoHealth.Color = Color.DarkOrange;
            des = healthIcon.Position + new Vector2(60, 10);
            textoHealth.Position = new Point((int)des.X, (int)des.Y);
            textoHealth.Size = new Size(3 * 24, 24);
            textoHealth.Align = TgcText2D.TextAlign.LEFT;

            font = new System.Drawing.Text.PrivateFontCollection();
            font.AddFontFile(MediaDir + "Fonts\\pdark.ttf");
            textoHealth.changeFont(new System.Drawing.Font(font.Families[0], 24, FontStyle.Bold));

            //texto de cuantas balas le quedan al jugador
            textoRecargas.Color = Color.DarkOrange;
            des = reloadsIcon.Position + new Vector2(60, 10);
            textoRecargas.Position = new Point((int)des.X, (int)des.Y);
            textoRecargas.Size = new Size(3 * 24, 24);
            textoRecargas.Align = TgcText2D.TextAlign.LEFT;

            font = new System.Drawing.Text.PrivateFontCollection();
            font.AddFontFile(MediaDir + "Fonts\\pdark.ttf");
            textoRecargas.changeFont(new System.Drawing.Font(font.Families[0], 24, FontStyle.Bold));
        }


        private void updateHealthBar(Player player)
        {
            var rectangulo = new Rectangle();

            rectangulo.X = healthBar.Bitmap.Size.Height;
            rectangulo.Y = healthBar.Bitmap.Size.Width;

            //updateo la barrita de vida
            int factor;
            if (player.Health != 0)
            {
                factor = (int)100 / player.Health;
                rectangulo.Width = (int)healthBar.Bitmap.Size.Width / factor;
            }
            else
            {
                rectangulo.Width = 0;
                factor = 0;
                healthBarEnabled = false;
            }

            rectangulo.Height = healthBar.Bitmap.Size.Height;
            healthBar.Scaling = new Vector2(0.75f, 0.22f);
            healthBar.SrcRect = rectangulo;
        }


        private void updateText(Player player)
        {
            textoHealth.Text = "" + player.Health;
            textoAmmo.Text = "" + player.Arma.Balas;
            textoRecargas.Text = "" + player.Arma.Recargas;
        }

    }

}
