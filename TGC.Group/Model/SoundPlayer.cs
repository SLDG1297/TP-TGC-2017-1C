using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.Input;
using TGC.Core.Sound;
using TGC.Group.Model.Entities;

namespace TGC.Group.Model
{
    public class SoundPlayer
    {
        private string musicFilePath;
        private TgcMp3Player mp3Player = new TgcMp3Player();

        private static SoundPlayer instance;

        public TgcStaticSound shootingGun;
        public TgcStaticSound clipin;
        public TgcStaticSound boltpull;

        public TgcDirectSound DirectSound;

        //Constructor privado para que nadie pueda instanciarlo
        private SoundPlayer() { }

        public static SoundPlayer Instance
        {
            get
            {
                if (instance == null) instance = new SoundPlayer();
                return instance;

            }
        }



        public void playMusic(string MediaDir, TgcDirectSound directsound)
        {
            this.DirectSound = directsound;
           
            var filePath = MediaDir + "Sound\\music\\Military.mp3";

            mp3Player.closeFile();
            mp3Player.FileName = filePath;

            mp3Player.play(true);
        }

        public void dispose()
        {
            mp3Player.stop();
            mp3Player.closeFile();

            if (shootingGun != null) shootingGun.dispose();
            if (clipin != null) clipin.dispose();
            if (boltpull != null) boltpull.dispose();
        }

        public void playPlayerSounds(string MediaDir,TgcD3dInput Input, Player player)
        {
            Arma arma = player.Arma;

            if (Input.keyPressed(Microsoft.DirectX.DirectInput.Key.R))
            {
                clipin = loadStaticSound(clipin, MediaDir, arma.reloadPath);
                clipin.play();
            }

            if (Input.buttonPressed(TgcD3dInput.MouseButtons.BUTTON_LEFT) || Input.buttonDown(TgcD3dInput.MouseButtons.BUTTON_LEFT))
            {
                if (player.Arma.Balas > 0)
                {
                    shootingGun = loadStaticSound(shootingGun, MediaDir, arma.shootPath);
                    shootingGun.play();
                }
                else
                {
                    boltpull = loadStaticSound(boltpull, MediaDir, arma.noBulletPath);
                    boltpull.play();
                }

            }
        }

        public TgcStaticSound loadStaticSound(TgcStaticSound sound,string MediaDir, string soundPath)
        {            
            string currentFile = null;

            var shootingPath = MediaDir + soundPath;

            if (currentFile == null || currentFile != shootingPath)
            {
                currentFile = shootingPath;

                //Borrar sonido anterior
                if (sound != null)
                {
                    sound.dispose();
                    sound = null;
                }
                //Cargar sonido
                sound = new TgcStaticSound();
                sound.loadSound(currentFile, this.DirectSound.DsDevice);
                
            }

            return sound;
        }
    }
}
