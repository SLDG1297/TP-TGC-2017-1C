using Microsoft.DirectX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGC.Core.BoundingVolumes;
using TGC.Core.Geometry;
using TGC.Core.SceneLoader;
using TGC.Core.Shaders;
using TGC.Core.Terrain;
using TGC.Core.Textures;
using TGC.Core.Utils;
using TGC.Group.Model.Collisions;
using Microsoft.DirectX.Direct3D;

namespace TGC.Group.Model
{
    public class World
    {
        private const int FACTOR = 8;

        // Constantes de escenario
        private const float MAP_SCALE_XZ = 160.0f; // Original = 20
        private const float MAP_SCALE_Y = 10.4f; // Original = 1.3
        // Esto se hace así porque ya hay valores hardcodeados de posiciones que no quiero cambiar.
        // Habría que ver una forma de ubicar meshes en posición relativa en el espacio.

        private Vector3 CENTRO = new Vector3(0, 0, 0);

        // Escenario
        private Terreno terreno;

        private TgcScene casa;
        private TgcMesh rocaOriginal;
        private TgcMesh palmeraOriginal;
        private TgcMesh pastito;
        private TgcMesh arbusto;
        private TgcMesh faraon;
        private TgcMesh arbolSelvatico;
        private TgcMesh hummer;

        private TgcMesh ametralladora;
        private TgcMesh ametralladora2;
        private TgcMesh canoa;
        private TgcMesh helicopter;
        private TgcMesh camionCisterna;
        private TgcMesh tractor;
        private TgcMesh barril;
        private TgcMesh cajaFuturistica;
        private TgcMesh avionMilitar;
        private TgcMesh tanqueFuturista;
        private TgcMesh avionCaza;

        private TgcBox cajita;
        private TgcMesh cajitaMuniciones;

        //objetos que se replican
        private List<TgcMesh> rocas = new List<TgcMesh>();
        private List<TgcMesh> palmeras = new List<TgcMesh>();
        private List<TgcMesh> pastitos = new List<TgcMesh>();
        private List<TgcMesh> cajitas = new List<TgcMesh>();
        private List<TgcMesh> arbolesSelvaticos = new List<TgcMesh>();
        private List<TgcMesh> arbustitos = new List<TgcMesh>();

        //lista de objetos totales
        private List<TgcMesh> meshes = new List<TgcMesh>();

        private CollisionManager collisionManager;
        private Effect vaivenX;
        private float time;

        /*public void initWorld(string MediaDir, Terreno terreno)
        {
            this.terreno = terreno;
            collisionManager = CollisionManager.Instance;

            initObjects(MediaDir);
            initializeList();
        }*/

        public void initWorld(string MediaDir, string ShadersDir, Terreno terreno)
        {
            this.terreno = terreno;
            collisionManager = CollisionManager.Instance;
            initObjects(MediaDir);
            initializeList();
            time = 0;
            initShaders(ShadersDir);
        }

        public void initShaders(string shadersDir)
        {
            vaivenX = TgcShaders.loadEffect(shadersDir + "VertexShader\\MovimientosBasicos.fx");
            canoa.Effect = vaivenX;
            canoa.Technique = "OndulacionZ";
            vaivenX.SetValue("amplitud_vaiven", 5);
        }

        private void initObjects(string MediaDir)
        {
            // Ubicación de la casa.
            string casaDir = MediaDir + "Meshes\\Edificios\\Casa\\Casa-TgcScene.xml";
            casa = cargarScene(casaDir);
            foreach (var mesh in casa.Meshes)
            {
                var position = new Vector3(-800 * FACTOR, 0, 1200 * FACTOR);
                mesh.Position = position;
                mesh.AutoTransformEnable = false;
                mesh.Transform = Matrix.Scaling(1.5f, 2f, 1.75f) * Matrix.RotationY(FastMath.PI_HALF + FastMath.PI) * Matrix.Translation(mesh.Position);
            }
            terreno.corregirAltura(casa.Meshes);

            // Creación de palmeras dispuestas circularmente.
            string palmeraDir = MediaDir + "Meshes\\Vegetation\\Palmera\\Palmera-TgcScene.xml";
            palmeraOriginal = cargarMesh(palmeraDir);
            Utils.disponerEnCirculoXZ(palmeraOriginal, palmeras, 200, 820 * FACTOR, 1);
            foreach (var palmera in palmeras)
            {
                palmera.AutoTransformEnable = false;
                palmera.Scale = new Vector3(1.5f, 1.5f, 1.5f);
                palmera.Transform = Matrix.Scaling(palmera.Scale) * palmera.Transform;
            }
            terreno.corregirAltura(palmeras);

            // Creación de pastitos.
            string pastitoDir = MediaDir + "Meshes\\Vegetation\\Pasto\\Pasto-TgcScene.xml";
            pastito = cargarMesh(pastitoDir);
            pastito.AlphaBlendEnable = true;
            pastito.Scale = new Vector3(0.5f, 0.5f, 0.5f);
            pastito.AutoTransformEnable = false;
            Utils.disponerEnRectanguloXZ(pastito, pastitos, 20, 20, 50);
            foreach (var pasto in pastitos)
            {
                var despl = new Vector3(0, 0, 8000);
                pasto.Position += despl;

                pasto.Transform = Matrix.Translation(pasto.Scale) * Matrix.Translation(pasto.Position);
            }
            //pongo los pastitos en aleatorio, pero los saco del circulo celeste del medio
            Utils.aleatorioXZExceptoRadioInicial(pastito, pastitos, 2500);

            terreno.corregirAltura(pastitos);

            //arbustitos
            arbusto = cargarMesh(MediaDir + "Meshes\\Vegetation\\Arbusto\\Arbusto-TgcScene.xml");
            arbusto.AlphaBlendEnable = true;
            arbusto.Scale = new Vector3(0.5f, 0.5f, 0.5f);
            pastito.AutoTransformEnable = false;
            Utils.aleatorioXZExceptoRadioInicial(arbusto, arbustitos, 2000);
            terreno.corregirAltura(arbustitos);

            // Creación de faraón.
            string faraonDir = MediaDir + "Meshes\\Objetos\\EstatuaFaraon\\EstatuaFaraon-TgcScene.xml";
            faraon = cargarMesh(faraonDir);
            faraon.AutoTransformEnable = false;
            faraon.Scale = new Vector3(FACTOR, FACTOR, FACTOR);
            faraon.Transform = Matrix.Scaling(faraon.Scale) * faraon.Transform;
            faraon.updateBoundingBox();

            // Creación de cajitas.
            cajita = TgcBox.fromSize(new Vector3(30 * FACTOR, 30 * FACTOR, 30 * FACTOR), TgcTexture.createTexture(MediaDir + "Texturas\\paja4.jpg"));
            Utils.disponerEnRectanguloXZ(cajita.toMesh("cajitaPaja"), cajitas, 2, 2, 250);
            foreach (var cajita in cajitas)
            {
                cajita.AutoTransformEnable = false;
                cajita.Position += new Vector3(-800 * FACTOR, 50, 1400 * FACTOR);
                cajita.Scale = new Vector3(0.25f, 0.25f, 0.25f);
                //cajita.Transform = Matrix.Scaling(cajita.Scale) * Matrix.Translation(cajita.Position) * cajita.Transform;
            }

            //cajitas de municiones
            var center = new Vector3(-12580, 1790, 9915);
            cajitaMuniciones = cargarMesh(MediaDir + "Meshes\\Armas\\CajaMuniciones\\CajaMuniciones-TgcScene.xml");
            cajitaMuniciones.AutoTransformEnable = false;
            cajitaMuniciones.createBoundingBox();
            cajitaMuniciones.updateBoundingBox();
            Utils.disponerEnCirculoXZ(cajitaMuniciones, cajitas, 8, 400, FastMath.QUARTER_PI, 0, center);
            Utils.aleatorioXZExceptoRadioInicial(cajitaMuniciones, cajitas, 25);

            //cajas futuristicas    
            cajaFuturistica = cargarMesh(MediaDir + "Meshes\\Objetos\\CajaMetalFuturistica2\\CajaMetalFuturistica2-TgcScene.xml");
            var cantCajitas = cajitas.Count;
            Utils.aleatorioXZExceptoRadioInicial(cajaFuturistica, cajitas, 25);
            terreno.corregirAltura(cajitas);

            for (int i = cantCajitas; i < cajitas.Count; i++)
            {
                var mesh = cajitas[i];
                mesh.Position += new Vector3(0, 50f, 0);
            }

            //ametralladora
            ametralladora2 = cargarMesh(MediaDir + "Meshes\\Armas\\MetralladoraFija2\\MetralladoraFija2-TgcScene.xml");
            ametralladora2.AutoTransformEnable = false;
            ametralladora2.Position = center;
            ametralladora2.Transform = Matrix.Translation(ametralladora2.Position) * ametralladora2.Transform;
            ametralladora2.createBoundingBox();
            ametralladora2.updateBoundingBox();

            //ametralladora
            ametralladora = cargarMesh(MediaDir + "Meshes\\Armas\\MetralladoraFija\\MetralladoraFija-TgcScene.xml");
            ametralladora.AutoTransformEnable = false;
            ametralladora.Position = new Vector3(1894, terreno.posicionEnTerreno(1894, 10793), 10793);
            ametralladora.Transform = Matrix.Translation(ametralladora.Position);

            //creacion de arboles selvaticos
            arbolSelvatico = cargarMesh(MediaDir + "Meshes\\Vegetation\\ArbolSelvatico\\ArbolSelvatico-TgcScene.xml");
            arbolSelvatico.Position = new Vector3(1000, 0, 400);
            arbolSelvatico.AutoTransformEnable = false;
            arbolSelvatico.Transform = Matrix.Translation(arbolSelvatico.Position) * arbolSelvatico.Transform;
            arbolSelvatico.createBoundingBox();
            arbolSelvatico.updateBoundingBox();

            //TODO: ajustar posicion segun heightmap (Hecho, aunque funciona mal todavía)
            // Frontera este de árboles
            Utils.disponerEnLineaX(arbolSelvatico, arbolesSelvaticos, 50, 450, new Vector3(-6000, terreno.posicionEnTerreno(-6000, 15200), 15200));

            //frontera sur de arboles
            var pos = arbolesSelvaticos.Last().Position;
            Utils.disponerEnLineaZ(arbolSelvatico, arbolesSelvaticos, 70, -450, arbolesSelvaticos.Last().Position);

            //frontera oeste
            Utils.disponerEnLineaX(arbolSelvatico, arbolesSelvaticos, 72, -450, arbolesSelvaticos.Last().Position);

            //frontera norte
            Utils.disponerEnLineaZ(arbolSelvatico, arbolesSelvaticos, 68, 450, arbolesSelvaticos.Last().Position);
            foreach (var arbol in arbolesSelvaticos)
            {
                arbol.Scale = new Vector3(3.0f, 3.0f, 3.0f);
                arbol.AutoTransformEnable = false;
                arbol.Transform = Matrix.Scaling(arbol.Scale) * arbol.Transform;
            }

            var rndm = new Random();
            var countArboles = arbolesSelvaticos.Count;
            Utils.aleatorioXZExceptoRadioInicial(arbolSelvatico, arbolesSelvaticos, 40);
            for (int i = countArboles; i <= arbolesSelvaticos.Count - 1; i++)
            {
                var s = rndm.Next(3, 6);
                var arbol = arbolesSelvaticos[i];

                arbol.AutoTransformEnable = false;
                arbol.Scale = new Vector3(s, s, s);
                arbol.Transform = Matrix.Scaling(arbol.Scale) * arbol.Transform;
            }
            terreno.corregirAltura(arbolesSelvaticos);

            // Creación de rocas.
            string rocaDir = MediaDir + "Meshes\\Vegetation\\Roca\\Roca-TgcScene.xml";
            rocaOriginal = cargarMesh(rocaDir);

            // Rocas en el agua.
            Utils.disponerEnCirculoXZ(rocaOriginal, rocas, 4, 500 * FACTOR, FastMath.PI_HALF);
            foreach (var roca in rocas)
            {
                roca.AutoTransformEnable = false;
                roca.Scale = new Vector3(3 * FACTOR, 2 * FACTOR, 3 * FACTOR);
                roca.Transform = Matrix.Scaling(roca.Scale) * roca.Transform;
            }

            // Frontera oeste de rocas.
            rocaOriginal.Position = new Vector3(1500, 0, -3000);
            rocaOriginal.Scale = new Vector3(4.0f, 4.0f, 4.0f);
            rocaOriginal.AutoTransformEnable = false;
            rocaOriginal.Transform = Matrix.Scaling(rocaOriginal.Scale) * Matrix.Translation(rocaOriginal.Position) * rocaOriginal.Transform;

            rocaOriginal.createBoundingBox();
            rocaOriginal.updateBoundingBox();

            var count = rocas.Count;
            Utils.disponerEnLineaX(rocaOriginal, rocas, 49, -100, new Vector3(1500, 0, -3000));
            for (int i = count; i <= rocas.Count - 1; i++)
            {
                rocas[i].AutoTransformEnable = false;
                rocas[i].Scale = new Vector3(4.0f, 4.0f, 4.0f);
                rocas[i].Transform = Matrix.Scaling(rocas[i].Scale) * rocas[i].Transform;
            }
            terreno.corregirAltura(rocas);

            //barril
            barril = cargarMesh(MediaDir + "Meshes\\Objetos\\BarrilPolvora\\BarrilPolvora-TgcScene.xml");
            barril.Position = new Vector3(-6802, 8, 10985);
            barril.updateBoundingBox();

            // Autitos!
            hummer = cargarMesh(MediaDir + "Meshes\\Vehiculos\\Hummer\\Hummer-TgcScene.xml");
            hummer.Position = new Vector3(1754, terreno.posicionEnTerreno(1754, 9723), 9723);
            hummer.Scale = new Vector3(1.1f, 1.08f, 1.25f);
            hummer.AutoTransformEnable = false;
            hummer.Transform = Matrix.Scaling(hummer.Scale) * Matrix.Translation(hummer.Position) * hummer.Transform;
            hummer.createBoundingBox();
            hummer.updateBoundingBox();

            var anotherHummer = hummer.createMeshInstance(hummer.Name + "1");
            anotherHummer.Position = new Vector3(hummer.Position.X + 350, hummer.Position.Y, hummer.Position.Z - 150);
            anotherHummer.Scale = hummer.Scale;
            anotherHummer.rotateY(FastMath.QUARTER_PI);
            anotherHummer.AutoTransformEnable = false;
            anotherHummer.Transform = Matrix.RotationY(anotherHummer.Rotation.Y)
                                       * Matrix.Scaling(anotherHummer.Scale)
                                       * Matrix.Translation(anotherHummer.Position)
                                       * anotherHummer.Transform;
            anotherHummer.createBoundingBox();
            anotherHummer.updateBoundingBox();

            meshes.Add(anotherHummer);

            //helicoptero
            helicopter = cargarMesh(MediaDir + "Meshes\\Vehiculos\\HelicopteroMilitar\\HelicopteroMilitar-TgcScene.xml");
            helicopter.Position = new Vector3(8308, 0, -4263);
            helicopter.AutoTransformEnable = false;
            helicopter.Scale = new Vector3(4f, 4f, 4f);
            helicopter.Transform = Matrix.Scaling(helicopter.Scale) * Matrix.Translation(helicopter.Position) * helicopter.Transform;
            helicopter.createBoundingBox();
            helicopter.BoundingBox.transform(Matrix.Scaling(0.8f, 2.25f, 3.55f) * Matrix.Translation(helicopter.Position));

            //tanque
            tanqueFuturista = cargarMesh(MediaDir + "Meshes\\Vehiculos\\TanqueFuturistaRuedas\\TanqueFuturistaRuedas-TgcScene.xml");
            tanqueFuturista.Position = new Vector3(11000, terreno.posicionEnTerreno(11000, 6295), 6295);
            tanqueFuturista.Scale = new Vector3(3f, 3f, 3f);
            tanqueFuturista.updateBoundingBox();

            //agrego otra instancia del tanque, la desplazo, roto y ajusto su posicion en Y
            var anotherTank = tanqueFuturista.createMeshInstance(tanqueFuturista.Name + "1");
            var posTanque2 = tanqueFuturista.Position + new Vector3(650, 0, -450);
            anotherTank.Position = new Vector3(posTanque2.X, terreno.posicionEnTerreno(posTanque2.X, posTanque2.Z), posTanque2.Z);
            anotherTank.Scale = tanqueFuturista.Scale;
            anotherTank.rotateY(FastMath.PI_HALF);

            anotherTank.AutoTransformEnable = false;
            anotherTank.Transform = Matrix.RotationY(anotherTank.Rotation.Y)
                                       * Matrix.Scaling(anotherTank.Scale)
                                       * Matrix.Translation(anotherTank.Position);
            anotherTank.createBoundingBox();
            //acutalizo el bounding box
            anotherTank.BoundingBox.transform(anotherTank.Transform);
            meshes.Add(anotherTank);

            //avion militar
            avionMilitar = cargarMesh(MediaDir + "Meshes\\Vehiculos\\AvionMilitar\\AvionMilitar-TgcScene.xml");
            avionMilitar.Position = hummer.Position + new Vector3(1050, 50, 250);
            helicopter.AutoTransformEnable = false;
            //escalo,roto y traslado
            avionMilitar.Scale = new Vector3(2f, 2f, 2f);
            avionMilitar.rotateY(FastMath.PI_HALF);
            avionMilitar.Transform = Matrix.RotationY(avionMilitar.Rotation.Y) * Matrix.Scaling(avionMilitar.Scale) * Matrix.Translation(avionMilitar.Position);
            avionMilitar.createBoundingBox();

            avionMilitar.BoundingBox.transform(Matrix.RotationY(avionMilitar.Rotation.Y)
                                            * Matrix.Scaling(2f, 1.2f, 0.3f)
                                            * Matrix.Translation(avionMilitar.Position));


            //avion de caza
            avionCaza = cargarMesh(MediaDir + "Meshes\\Vehiculos\\AvionCaza\\AvionCaza-TgcScene.xml");
            avionCaza.Position = new Vector3(3423, 3000, -3847);
            avionCaza.updateBoundingBox();

            //tractor
            tractor = cargarMesh(MediaDir + "Meshes\\Vehiculos\\Tractor\\Tractor-TgcScene.xml");
            tractor.Position = new Vector3(-6802, 0, 10385);
            tractor.Scale = new Vector3(1.5f, 1f, 1.25f);
            tractor.AutoTransformEnable = false;
            tractor.Transform = Matrix.Translation(tractor.Position) * tractor.Transform;
            tractor.updateBoundingBox();

            //canoa
            canoa = cargarMesh(MediaDir + "Meshes\\Vehiculos\\Canoa\\Canoa-TgcScene.xml");
            canoa.Position = new Vector3(3423, 10, -3847);
            canoa.updateBoundingBox();


            //camionCisterna
            camionCisterna = cargarMesh(MediaDir + "Meshes\\Vehiculos\\CamionCisterna\\CamionCisterna-TgcScene.xml");
            helicopter.AutoTransformEnable = false;
            camionCisterna.Position = new Vector3(227, 0, 10719);
            camionCisterna.Scale = new Vector3(2f, 2f, 2f);
            camionCisterna.Transform = Matrix.Scaling(camionCisterna.Scale) * Matrix.Translation(camionCisterna.Position) * camionCisterna.Transform;
            camionCisterna.updateBoundingBox();
        }

        private void initializeList()
        {
            meshes.Add(arbolSelvatico);
            meshes.Add(hummer);
            meshes.Add(ametralladora);
            meshes.Add(ametralladora2);
            meshes.Add(canoa);
            meshes.Add(helicopter);
            meshes.Add(camionCisterna);
            meshes.Add(tractor);
            meshes.Add(barril);
            meshes.Add(faraon);
            meshes.Add(avionMilitar);
            meshes.Add(tanqueFuturista);
            meshes.Add(avionCaza);

            meshes.AddRange(pastitos);
            meshes.AddRange(rocas);
            meshes.AddRange(palmeras);
            meshes.AddRange(arbolesSelvaticos);
            meshes.AddRange(arbustitos);
            meshes.AddRange(cajitas);
        }

        public void disposeWorld()
        {
            casa.disposeAll();

            rocaOriginal.dispose();
            palmeraOriginal.dispose();
            pastito.dispose();
            faraon.dispose();
            arbolSelvatico.dispose();
            cajita.dispose();
            hummer.dispose();
            canoa.dispose();
            ametralladora2.dispose();
            camionCisterna.dispose();
            helicopter.dispose();
            avionMilitar.dispose();
            tractor.dispose();
            barril.dispose();
            arbusto.dispose();
            ametralladora.dispose();
            tanqueFuturista.dispose();
            avionCaza.dispose();

            meshes.Clear();
            rocas.Clear();
            pastitos.Clear();
            arbolesSelvaticos.Clear();
            palmeras.Clear();
            cajitas.Clear();

            //dispose de efectos
            vaivenX.Dispose();
        }

        public void initObstaculos()
        {
            //Añadir escenario.
            //aniadirObstaculoAABB(casa.Meshes);            

            //bounging cilinders de las palmeras
            foreach (var palmera in palmeras)
            {
                var despl = new Vector3(0, 100, 0);
                var cilindro = new TgcBoundingCylinderFixedY(
                    palmera.Position + despl, 20, 100);

                collisionManager.agregarCylinder(cilindro);
            }

            //bounding cyilinder del arbol
            var adjustPos = new Vector3(0, 0, 44);
            //var cylinder = new TgcBoundingCylinderFixedY(arbolSelvatico.BoundingBox.calculateBoxCenter(), 60, 200);
            var cylinder = new TgcBoundingCylinderFixedY(arbolSelvatico.BoundingBox.calculateBoxCenter(), 60, 200);
            CollisionManager.Instance.agregarCylinder(cylinder);

            //bounding cilinders de los arbolesSelvaticos
            foreach (var arbol in arbolesSelvaticos)
            {
                var despl = new Vector3(0, 400, 0);

                arbol.Transform = Matrix.Scaling(arbol.Scale) * Matrix.Translation(arbol.Position);
                arbol.createBoundingBox();
                arbol.updateBoundingBox();

                var radio = 60 * arbol.Scale.X;
                var height = 200 * arbol.Scale.Y;
                var cilindro = new TgcBoundingCylinderFixedY(arbol.BoundingBox.calculateBoxCenter(), radio, height);

                //collisionManager.agregarAABB(arbol.BoundingBox);
                collisionManager.agregarCylinder(cilindro);
            }
            //bounding box de las rocas
            foreach (var roca in rocas)
            {
                var center = roca.BoundingBox.calculateBoxCenter();
                var radio = roca.BoundingBox.calculateAxisRadius();

                var cilindro = new TgcBoundingCylinderFixedY(center, radio.X * 0.95f, radio.Y);

                collisionManager.agregarCylinder(cilindro);
            }

            //bounding cylinder del barril
            var barrilCylinder = new TgcBoundingCylinderFixedY(barril.BoundingBox.calculateBoxCenter(), barril.BoundingBox.calculateBoxRadius() - 18, 24);
            collisionManager.agregarCylinder(barrilCylinder);

            //cilindro del faraon del mesio
            var faraonCylinder = new TgcBoundingCylinderFixedY(faraon.BoundingBox.calculateBoxCenter(), faraon.BoundingBox.calculateBoxRadius() * 0.15f, 1500);
            collisionManager.agregarCylinder(faraonCylinder);

            collisionManager.agregarAABB(canoa.BoundingBox);
            collisionManager.agregarAABB(helicopter.BoundingBox);
            collisionManager.agregarAABB(camionCisterna.BoundingBox);
            collisionManager.agregarAABB(tractor.BoundingBox);

            collisionManager.agregarAABB(tanqueFuturista.BoundingBox);
            foreach (var mesh in tanqueFuturista.MeshInstances)
            {
                collisionManager.agregarAABB(mesh.BoundingBox);
            }

            //bounding box de las hummer
            collisionManager.agregarAABB(hummer.BoundingBox);
            foreach (var mesh in hummer.MeshInstances)
            {
                var obb = new TgcBoundingOrientedBox();

                obb.Center = mesh.BoundingBox.calculateBoxCenter();
                obb.Extents = mesh.BoundingBox.calculateAxisRadius();

                obb.setRotation(new Vector3(0, mesh.Rotation.Y, 0));
                collisionManager.agregarAABB(mesh.BoundingBox);
                collisionManager.agregarOBB(obb);
            }

            //bounding box del avion militar
            collisionManager.agregarAABB(avionMilitar.BoundingBox);
            //este seria el bb de las alas
            var otroBoundingBox = helicopter.BoundingBox.clone();
            otroBoundingBox.transform(Matrix.RotationY(avionMilitar.Rotation.Y)
                                             * Matrix.Scaling(0.3f, 1f, 2.3f)
                                             * Matrix.Translation(avionMilitar.Position - new Vector3(52, 0, 0)));
            collisionManager.agregarAABB(otroBoundingBox);

            //bounding box de las cajas
            foreach (var caja in cajitas)
            {
                caja.Transform = Matrix.Scaling(caja.Scale) * Matrix.Translation(caja.Position);
                caja.createBoundingBox();
                caja.updateBoundingBox();
                collisionManager.agregarAABB(caja.BoundingBox);
            }
        }

        public void updateWorld(float elapsedTime)
        {
            time += elapsedTime;
            // Cargar variables de shader, por ejemplo el tiempo transcurrido.
            vaivenX.SetValue("time", time);
            canoa.UpdateMeshTransform();
            canoa.AutoUpdateBoundingBox = false;
        }

        TgcMesh cargarMesh(string unaDireccion)
        {
            return cargarScene(unaDireccion).Meshes[0];
        }

        TgcScene cargarScene(string unaDireccion)
        {
            return new TgcSceneLoader().loadSceneFromFile(unaDireccion);
        }        
        
        public List<TgcMesh> Meshes
        {
            get { return meshes; }
        }
        
    }
}
