using Microsoft.DirectX;
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
        private TgcMp3Player mp3Player = new TgcMp3Player();

        private static SoundPlayer instance;       

        public TgcDirectSound DirectSound;

        public string mediaDir;
        public Tgc3dSound sound;
        public Tgc3dSound ambientSound;

        public float time = 0;
        public bool mute = true;

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
        
        public void initAndPlayMusic(string MediaDir, TgcDirectSound directsound, Personaje personaje){
            this.DirectSound = directsound;
            mediaDir = MediaDir;
            var filePath = MediaDir + "Sound\\music\\Military.mp3";

            mp3Player.closeFile();
            mp3Player.FileName = filePath;

            mp3Player.play(true);

            DirectSound.ListenerTracking = personaje.Esqueleto;
        }

        public void playMusic(string MediaDir, TgcDirectSound directsound)
        {
             this.DirectSound = directsound;
             var filePath = MediaDir + "Sound\\music\\Military.mp3";

             mp3Player.closeFile();
             mp3Player.FileName = filePath;

             mp3Player.play(true);
              mediaDir = MediaDir;           
        }

        public void dispose()
        {
            mp3Player.stop();
            mp3Player.closeFile();
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


        public void play3DSound(Vector3 position, string filePath)
        {
            if (sound != null) sound.dispose();
            sound = new Tgc3dSound(mediaDir + filePath, position, DirectSound.DsDevice);
            sound.MinDistance = 800f;
            
            sound.play();
        }

        public void playAmbientSound(float elapsedTime)
        {
            time = +elapsedTime;

            if(time > 300 && time < 303)
            {
                ambientSound = new Tgc3dSound("Sound\\ambient\\Birds1.wav",new Vector3(800, 0, 1000), DirectSound.DsDevice);
                ambientSound.MinDistance = 400f;

                ambientSound.play();
                time = 0;
            }
        }
    }
}
