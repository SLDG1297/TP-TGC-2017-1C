using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.BoundingVolumes;
using TGC.Core.Particle;
using TGC.Core.SceneLoader;
using TGC.Core.Utils;
using TGC.Group.Model.Collisions;
using TGC.Group.Model.Entities;

namespace TGC.Group.Model.Environment
{
    public abstract class Explosivo
    {
        protected TgcMesh mesh;
        protected float radioExplosion;
        protected Vector3 position;
        protected int danio;
        protected string explosionPath;

        protected string particlePath;
        protected ParticleEmitter emitter;
        public bool exploto = false;


        public void explotar(List<Personaje> personajes)
        {
            //si todavia no exploto, desencadeno acciones
            if (!exploto)
            {                
                //reproduzco el sonido
                SoundPlayer.Instance.play3DSound(position, explosionPath);

                var lista = personajes.FindAll(p => estaDentroDelRadio(p));

                //quito vidas a los que esten en el radio de explosion
                if (lista.Count > 0)
                {
                    foreach (var p in lista)
                    {
                        p.recibiDanio(danio);
                    }
                }                

                //inicializo el emisor de particulas
                emitter = new ParticleEmitter(particlePath, 10);
                emitter.Position = position;

                emitter.MinSizeParticle = 6;
                emitter.MaxSizeParticle = 10;
                emitter.ParticleTimeToLive = 0.7750f;
                emitter.CreationFrecuency = 0.6250f;
                emitter.Dispersion = 200;
                emitter.Speed = new Vector3(0, 10, 0);

                exploto = true;
            }            
        }

        private bool estaDentroDelRadio(Personaje personaje)
        {
            return FastMath.Pow2(position.X - personaje.Position.X) 
                    + FastMath.Pow2(position.Z - personaje.Position.Z)
                    <= FastMath.Pow2(radioExplosion);
        }

        public void update()
        {
        }
        
        public abstract void createBoundingVolume();

        public TgcMesh Mesh
        {
            get { return mesh; }
        }
    }

    public class Barril : Explosivo
    {
        private TgcBoundingCylinderFixedY boundingCylinder;

        public Barril(string MediaDir, Vector3 initPos)
        {
            var sceneloader = new TgcSceneLoader();
            //barril
            mesh = sceneloader.loadSceneFromFile(MediaDir + "Meshes\\Objetos\\BarrilPolvora\\BarrilPolvora-TgcScene.xml").Meshes[0];
            mesh.Position = initPos;

            position = initPos;

            danio = 40;
            radioExplosion = 500;

            explosionPath = "Sound\\ambient\\barrell-explosion.wav";

            particlePath = MediaDir + "Texturas\\Particles\\fuego.png";
        }

        public override void createBoundingVolume()
        {
            this.boundingCylinder = new TgcBoundingCylinderFixedY(mesh.BoundingBox.calculateBoxCenter(),
                                            mesh.BoundingBox.calculateBoxRadius() - 18, 24);
            
            CollisionManager.Instance.agregarBarril(this);
        }
        
        public TgcBoundingCylinderFixedY BoundingCylinder
        {
            get { return boundingCylinder; }
        }

        public void render(float elapsedTime)
        {
            mesh.render();
            if (exploto)
            {
                emitter.render(elapsedTime);
            }
        }

        public void dispose()
        {
            mesh.dispose();
            boundingCylinder.dispose();

            if(emitter != null)
            {
                emitter.dispose();
            }
        }
    }
}
 