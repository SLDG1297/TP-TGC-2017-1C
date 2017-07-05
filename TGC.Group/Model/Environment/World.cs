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
using TGC.Group.Model.Optimization;
using TGC.Core.Camara;
using TGC.Core.Direct3D;
using System.Drawing;

namespace TGC.Group.Model.Environment
{
    public class World
    {
        private const int FACTOR = 8;

        // Constantes de escenario
        private const float MAP_SCALE_XZ = 80f; // Original = 20
        private const float MAP_SCALE_Y = 10.4f; // Original = 1.3
        // Esto se hace así porque ya hay valores hardcodeados de posiciones que no quiero cambiar.
        // Habría que ver una forma de ubicar meshes en posición relativa en el espacio.

        private Vector3 CENTRO = new Vector3(0, 0, 0);

        // Escenario
        private Terreno terreno;

        private TgcScene isla;
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
        private TgcMesh cajaFuturistica;
        private TgcMesh avionMilitar;
        private TgcMesh tanqueFuturista;
        private TgcMesh avionCaza;
        private TgcMesh barril;

        private TgcMesh piso;
        private float nivel_mar;
        private CubeTexture g_pCubeMapAgua;

        private List<Barril> barrilesExplosivos = new List<Barril>();

        private TgcBox cajita;
        private TgcMesh cajitaMuniciones;

        //objetos que se replican
        private List<TgcMesh> rocas = new List<TgcMesh>();
        private List<TgcMesh> palmeras = new List<TgcMesh>();
        private List<TgcMesh> pastitos = new List<TgcMesh>();
        private List<TgcMesh> cajitas = new List<TgcMesh>();
        private List<TgcMesh> arbolesSelvaticos = new List<TgcMesh>();
        private List<TgcMesh> arbustitos = new List<TgcMesh>();
        private List<TgcMesh> barriles = new List<TgcMesh>();

        //lista de objetos totales
        private List<TgcMesh> meshes = new List<TgcMesh>();

        private CollisionManager collisionManager;
        private Effect vaiven;
        private Effect viento;
        private Effect envmap;

        private float time;
        private TgcMesh helicopter2;
        private TgcMesh helicopter3;

        public void initWorld(string MediaDir, string ShadersDir, Terreno terreno)
        {
            this.terreno = terreno;
            collisionManager = CollisionManager.Instance;
            initObjects(MediaDir);
            initializeList();
            
            initShaders(ShadersDir);
        }

        public void initShaders(string shadersDir)
        {
            vaiven = TgcShaders.loadEffect(shadersDir + "VertexShader\\MovimientosBasicos.fx");
            canoa.Effect = vaiven;
            canoa.Technique = "OndulacionZ";
            vaiven.SetValue("amplitud_vaiven", 5);
            
            avionCaza.Effect = vaiven;
            avionCaza.Technique = "CirculoXZ";

            viento = TgcShaders.loadEffect(shadersDir + "VertexShader\\Wind.fx");
            
            foreach(var pasto in pastitos)
            {
                pasto.Effect = viento;
                pasto.Technique = "Wind";
            }

            foreach (var arbusto in arbustitos)
            {
                arbusto.Effect = viento;
                arbusto.Technique = "Wind";
            }

            foreach (var arbol in arbolesSelvaticos)
            {
                arbol.Effect = viento;
                arbol.Technique = "Wind";
            }

            envmap = TgcShaders.loadEffect(shadersDir + "Demo.fx");
            piso.Effect = envmap;
            piso.Technique = "RenderScene";
            time = 0;

            avionMilitar.Effect = envmap;
            avionMilitar.Technique = "RenderScene";

            tanqueFuturista.Effect = envmap;
            tanqueFuturista.Technique = "RenderScene";


            helicopter3.Effect = vaiven;
            helicopter3.Technique = "OndulacionZ";
            vaiven.SetValue("amplitud_vaiven", 100);
        }

        private void initObjects(string MediaDir)
        {
            // Ubicación de la casa.
            string islaDir = MediaDir + "Meshes\\Scenes\\Isla\\Isla-TgcScene.xml";
            isla = cargarScene(islaDir);
            foreach (var mesh in isla.Meshes)
            {
                mesh.Scale = new Vector3(8, 8, 8);
                mesh.move(0, -15, 0);
                mesh.updateBoundingBox();
            }
            //terreno.corregirAltura(casa.Meshes);
            

            // Creación de palmeras dispuestas circularmente.
            string palmeraDir = MediaDir + "Meshes\\Vegetation\\Palmera\\Palmera-TgcScene.xml";
            palmeraOriginal = cargarMesh(palmeraDir);
            Utils.disponerEnCirculoXZ(palmeraOriginal, palmeras, 100, 820 * FACTOR, 100/ FastMath.ToRad(30) );
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
            pastito.Scale = new Vector3(2f, 2f, 2f);
            pastito.AutoTransformEnable = false;
            //Utils.disponerEnRectanguloXZ(pastito, pastitos, 30, 30, 250);
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
            arbusto.Scale = new Vector3(2f, 2f, 2f);
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
            cajaFuturistica.AutoTransformEnable = false;
            cajaFuturistica.createBoundingBox();
            cajaFuturistica.updateBoundingBox();
            var cantCajitas = cajitas.Count;
            Utils.aleatorioXZExceptoRadioInicial(cajaFuturistica, cajitas, 75);
            terreno.corregirAltura(cajitas);

            for (int i = cantCajitas; i < cajitas.Count; i++)
            {
                var mesh = cajitas[i];
                mesh.Position += new Vector3(0, 50f, 0);
                //mesh.Transform = Matrix.Translation(mesh.Position) * Matrix.Scaling(mesh.Scale);
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
            Utils.disponerEnLineaX(arbolSelvatico, arbolesSelvaticos, 25, 900, new Vector3(-6000, terreno.posicionEnTerreno(-6000, 15200), 15200));

            //frontera sur de arboles
            var pos = arbolesSelvaticos.Last().Position;
            Utils.disponerEnLineaZ(arbolSelvatico, arbolesSelvaticos, 35, -900, arbolesSelvaticos.Last().Position);

            //frontera oeste
            Utils.disponerEnLineaX(arbolSelvatico, arbolesSelvaticos, 36, -900, arbolesSelvaticos.Last().Position);

            //frontera norte
            Utils.disponerEnLineaZ(arbolSelvatico, arbolesSelvaticos, 34, 900, arbolesSelvaticos.Last().Position);
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
            rocaOriginal = cargarMesh(MediaDir + "Meshes\\Vegetation\\Roca\\Roca-TgcScene.xml");
            rocaOriginal.AutoTransformEnable = false;

            // Rocas en el agua.
            Utils.disponerEnCirculoXZ(rocaOriginal, rocas, 4, 500 * FACTOR, FastMath.PI_HALF);
            foreach (var roca in rocas)
            {
                roca.AutoTransformEnable = false;
                roca.Scale = new Vector3(3 * FACTOR, 2 * FACTOR, 3 * FACTOR);
                roca.Transform = Matrix.Scaling(roca.Scale) * roca.Transform;
            }

            //barriles - son explosivos
            barril = cargarMesh(MediaDir + "Meshes\\Objetos\\BarrilPolvora\\BarrilPolvora-TgcScene.xml");
            barril.Position = new Vector3(-6802, 8, 10985);
            barril.updateBoundingBox();
            //barril = new Barril(MediaDir , new Vector3(-6802, 8, 10985),
            barriles.Add(barril);

            Utils.aleatorioXZExceptoRadioInicial(barril,barriles, 35);
            terreno.corregirAltura(barriles);

            foreach (var barril in barriles)
            {
                barril.AutoTransformEnable = false;
                //lo hago antes porque necesito el mediaDir
                barril.createBoundingBox();
                barril.updateBoundingBox();

                var barrilExplosivo = new Barril(MediaDir, barril.Position, barril);
                barrilExplosivo.createBoundingVolume();
                barrilesExplosivos.Add(barrilExplosivo);
            }

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
            avionCaza.AutoTransformEnable = false;
            avionCaza.Transform = Matrix.Translation(avionCaza.Position);
            avionCaza.updateBoundingBox();


            helicopter2 = cargarMesh(MediaDir + "Meshes\\Vehiculos\\HelicopteroMilitar2\\HelicopteroMilitar2-TgcScene.xml");
            helicopter2.Position = new Vector3(-11900, 2090, 11960);
            helicopter2.rotateY(-FastMath.PI_HALF);
            helicopter2.Scale = new Vector3(3f,3f,3f);
            helicopter2.AutoTransformEnable = false;
            terreno.corregirAltura(helicopter2);
            helicopter2.Transform = Matrix.RotationY(helicopter2.Rotation.Y)
                                    * Matrix.Scaling(helicopter2.Scale)
                                    * Matrix.Translation(helicopter2.Position);

            helicopter2.createBoundingBox();
            helicopter.BoundingBox.transform(Matrix.RotationY(helicopter2.Rotation.Y)
                                    * Matrix.Scaling(helicopter2.Scale)
                                    * Matrix.Translation(helicopter2.Position));
            helicopter2.updateBoundingBox();


            helicopter3 = cargarMesh(MediaDir + "Meshes\\Vehiculos\\HelicopteroMilitar3\\HelicopteroMilitar3-TgcScene.xml");
            helicopter3.Position = new Vector3(-12410, 3090, 14193);
            helicopter3.AutoTransformEnable = false;
            //terreno.corregirAltura(helicopter3);
            helicopter3.Transform = Matrix.Translation(helicopter3.Position);
            helicopter3.updateBoundingBox();

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

            //agua
            piso = cargarMesh(MediaDir + "Meshes\\Piso\\Agua-TgcScene.xml");
            nivel_mar = 8;
            piso.Scale = new Vector3(50f, 1f, 50f);
            //piso.Scale = new Vector3(20f, 1f, 20f);
            //piso.Position = new Vector3(11382, nivel_mar + 40, 8885);
            piso.Position = new Vector3(0, nivel_mar, 0);
            piso.AutoTransformEnable = false;
            piso.Transform = Matrix.Scaling(piso.Scale) * Matrix.Translation(piso.Position);
            piso.updateBoundingBox();

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
            meshes.Add(helicopter3);

            //meshes.Add(piso);
            //meshes.Add(barril.Mesh);
            //meshes.Add(faraon);
            meshes.Add(helicopter2);
            meshes.Add(avionMilitar);
            meshes.Add(tanqueFuturista);
            meshes.Add(avionCaza);
            
            meshes.AddRange(pastitos);
            meshes.AddRange(rocas);
            meshes.AddRange(palmeras);
            meshes.AddRange(arbolesSelvaticos);
            meshes.AddRange(arbustitos);
            meshes.AddRange(cajitas);
            meshes.AddRange(barriles);
            meshes.AddRange(isla.Meshes);
        }

        public void disposeWorld()
        {
            isla.disposeAll();

            rocaOriginal.dispose();
            palmeraOriginal.dispose();
            pastito.dispose();
            arbolSelvatico.dispose();
            arbusto.dispose();
            faraon.dispose();

            cajita.dispose();

            hummer.dispose();
            canoa.dispose();
            ametralladora2.dispose();
            camionCisterna.dispose();
            helicopter.dispose();
            helicopter2.dispose();
            helicopter3.dispose();
            avionMilitar.dispose();
            tractor.dispose();

            barril.dispose();
            ametralladora.dispose();
            tanqueFuturista.dispose();
            avionCaza.dispose();

            meshes.Clear();
            rocas.Clear();
            pastitos.Clear();
            arbolesSelvaticos.Clear();
            palmeras.Clear();
            cajitas.Clear();
            piso.dispose();

            isla.disposeAll();
            foreach(var barril in barrilesExplosivos)
            {
               barril.dispose();
            }

            //dispose de efectos
            vaiven.Dispose();
            viento.Dispose();
            envmap.Dispose();
        }

        public void initObstaculos()
        {
            //Añadir escenario.      

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
                roca.createBoundingBox();
                roca.updateBoundingBox();
                var cilindro = new TgcBoundingCylinderFixedY(center, radio.X * 0.95f, radio.Y);

                collisionManager.agregarCylinder(cilindro);
            };

            //cilindro de la isla del centro
            var centerCylinder = new TgcBoundingCylinderFixedY( new Vector3(0,0,0), 1800, 200);
            collisionManager.agregarCylinder(centerCylinder);

            collisionManager.agregarAABB(canoa.BoundingBox);
            collisionManager.agregarAABB(helicopter.BoundingBox);
            collisionManager.agregarAABB(helicopter2.BoundingBox);
            collisionManager.agregarAABB(camionCisterna.BoundingBox);
            collisionManager.agregarAABB(tractor.BoundingBox);            
            collisionManager.agregarAABB(avionCaza.BoundingBox);

            collisionManager.agregarAABB(tanqueFuturista.BoundingBox);
            foreach (var mesh in tanqueFuturista.MeshInstances)
            {
                tanqueFuturista.updateBoundingBox();
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
                //caja.createBoundingBox();
                caja.updateBoundingBox();
                collisionManager.agregarAABB(caja.BoundingBox);
            }
        }

        public void updateWorld(float elapsedTime)
        {
            time += elapsedTime;
            //foreach(var barril in barriles) barril.update();

            // Cargar variables de shader, por ejemplo el tiempo transcurrido.
            vaiven.SetValue("time", time);
            viento.SetValue("time", time);

            canoa.UpdateMeshTransform();

            vaiven.SetValue("rotationTime", time);
            avionCaza.UpdateMeshTransform();

            //esto es para actualizar la posicion del bounding box
            var newPos = avionCaza.Position + new Vector3(1000 * FastMath.Sin(time), 0, 1000 * FastMath.Cos(time));
            avionCaza.BoundingBox.scaleTranslate(newPos, new Vector3(1, 1, 1));           
           
        }

        public void restoreEffect()
        {
            foreach (var mesh in meshes)
            {
                mesh.Effect = viento;
                mesh.Technique = "DefaultTechnique";
            }

            //tanqueFuturista.Effect = envmap;
            //tanqueFuturista.Technique = "RenderSceme";

            foreach (var pasto in pastitos)
            {
                pasto.Effect = viento;
                pasto.Technique = "Wind";
            }

            foreach (var arbusto in arbustitos)
            {
                arbusto.Effect = viento;
                arbusto.Technique = "Wind";
            }

            foreach (var arbol in arbolesSelvaticos)
            {
                arbol.Effect = viento;
                arbol.Technique = "Wind";
            }
            canoa.Effect = vaiven;
            canoa.Technique = "OndulacionZ";
            vaiven.SetValue("amplitud_vaiven", 5);

            avionCaza.Effect = vaiven;
            avionCaza.Technique = "CirculoXZ";

            piso.Effect = envmap;
            piso.Technique = "RenderAgua";
        }
        
        public void initRenderEnvMap(TgcFrustum Frustum, float ElapsedTime, TgcCamera camera, TgcSkyBox skybox)
        {
            initRenderLagos(skybox, Frustum);
            //if (RenderUtils.estaDentroDelFrustum(tanqueFuturista, Frustum))
            //{
            //        renderEnvMap(tanqueFuturista, ElapsedTime, camera, skybox);
            //}   
        }
        
        public void renderEnvMap(TgcMesh mesh, float ElapsedTime, TgcCamera camera, TgcSkyBox skybox)
        {           
                var aspectRatio = D3DDevice.Instance.AspectRatio;
                 // Creo el env map del tanque:
                 var g_pCubeMap = new CubeTexture(D3DDevice.Instance.Device, 256, 1, Usage.RenderTarget,
                                Format.A16B16G16R16F, Pool.Default);
                var pOldRT = D3DDevice.Instance.Device.GetRenderTarget(0);
                // ojo: es fundamental que el fov sea de 90 grados.
                // asi que re-genero la matriz de proyeccion
                D3DDevice.Instance.Device.Transform.Projection =
                    Matrix.PerspectiveFovLH(Geometry.DegreeToRadian(90.0f), 1f, 1f, 10000f);

                // Genero las caras del enviroment map
                for (var nFace = CubeMapFace.PositiveX; nFace <= CubeMapFace.NegativeZ; ++nFace)
                {
                    var pFace = g_pCubeMap.GetCubeMapSurface(nFace, 0);
                    D3DDevice.Instance.Device.SetRenderTarget(0, pFace);
                    Vector3 Dir, VUP;
                    Color color;
                    switch (nFace)
                    {
                        default:
                        case CubeMapFace.PositiveX:
                            // Left
                            Dir = new Vector3(1, 0, 0);
                            VUP = new Vector3(0, 1, 0);
                            color = Color.Black;
                            break;

                        case CubeMapFace.NegativeX:
                            // Right
                            Dir = new Vector3(-1, 0, 0);
                            VUP = new Vector3(0, 1, 0);
                            color = Color.Red;
                            break;

                        case CubeMapFace.PositiveY:
                            // Up
                            Dir = new Vector3(0, 1, 0);
                            VUP = new Vector3(0, 0, -1);
                            color = Color.Gray;
                            break;

                        case CubeMapFace.NegativeY:
                            // Down
                            Dir = new Vector3(0, -1, 0);
                            VUP = new Vector3(0, 0, 1);
                            color = Color.Yellow;
                            break;

                        case CubeMapFace.PositiveZ:
                            // Front
                            Dir = new Vector3(0, 0, 1);
                            VUP = new Vector3(0, 1, 0);
                            color = Color.Green;
                            break;

                        case CubeMapFace.NegativeZ:
                            // Back
                            Dir = new Vector3(0, 0, -1);
                            VUP = new Vector3(0, 1, 0);
                            color = Color.Blue;
                            break;
                    }

                    //Obtener ViewMatrix haciendo un LookAt desde la posicion final anterior al centro de la camara
                    var Pos = tanqueFuturista.Position;
                    D3DDevice.Instance.Device.Transform.View = Matrix.LookAtLH(Pos, Pos + Dir, VUP);

                    D3DDevice.Instance.Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, color, 1.0f, 0);

                    D3DDevice.Instance.Device.BeginScene();
                    //Renderizar
                    terreno.render();
                    skybox.render();
                    foreach (var meshcito in meshes)
                    {
                        if (FastMath.Pow2(mesh.Position.X - meshcito.Position.X) +
                            FastMath.Pow2(mesh.Position.Z - meshcito.Position.Z)
                            <= FastMath.Pow2(500)  && meshcito.Position != mesh.Position)
                        {
                            meshcito.render();
                        }
                    }
                    D3DDevice.Instance.Device.EndScene();
                }

                // restuaro el render target
                D3DDevice.Instance.Device.SetRenderTarget(0, pOldRT);

                D3DDevice.Instance.Device.Transform.View = camera.GetViewMatrix();

                D3DDevice.Instance.Device.Transform.Projection =
                    Matrix.PerspectiveFovLH(Geometry.DegreeToRadian(45.0f),
                                aspectRatio, 1f, 10000f);

                // Cargo las var. del shader:
                envmap.SetValue("g_txCubeMap", g_pCubeMap);
                envmap.SetValue("fvLightPosition", new Vector4(4000f, 8000f, 3000f, 0f));
                envmap.SetValue("fvEyePosition",
                    TgcParserUtils.vector3ToFloat3Array(camera.Position));
                envmap.SetValue("time", time);

                mesh.Technique = "RenderCubeMap";

                envmap.SetValue("g_txCubeMap", g_pCubeMap);
                g_pCubeMap.Dispose();
        }

        public void initRenderLagos(TgcSkyBox skyBox, TgcFrustum frustum)
        {
            piso.Effect = envmap;
            piso.Technique = "RenderScene";

            if (RenderUtils.estaDentroDelFrustum(piso,frustum) && g_pCubeMapAgua == null )
            {
                // solo la primera vez crea el env map del agua
                CrearEnvMapAgua(skyBox);
                // ya que esta creado, se lo asigno al effecto:
                envmap.SetValue("g_txCubeMapAgua", g_pCubeMapAgua);
            }

        }

        public void CrearEnvMapAgua(TgcSkyBox skyBox)
        {

            var aspectRatio = D3DDevice.Instance.AspectRatio;
            // creo el enviroment map para el agua
            g_pCubeMapAgua = new CubeTexture(D3DDevice.Instance.Device, 256, 1, Usage.RenderTarget,
                Format.A16B16G16R16F, Pool.Default);
            var pOldRT = D3DDevice.Instance.Device.GetRenderTarget(0);
            // ojo: es fundamental que el fov sea de 90 grados.
            // asi que re-genero la matriz de proyeccion
            D3DDevice.Instance.Device.Transform.Projection =
                Matrix.PerspectiveFovLH(Geometry.DegreeToRadian(90.0f),
                    aspectRatio, 1f, 100000f);
            // Genero las caras del enviroment map
            for (var nFace = CubeMapFace.PositiveX; nFace <= CubeMapFace.NegativeZ; ++nFace)
            {
                var pFace = g_pCubeMapAgua.GetCubeMapSurface(nFace, 0);
                D3DDevice.Instance.Device.SetRenderTarget(0, pFace);
                Vector3 Dir, VUP;
                Color color;
                switch (nFace)
                {
                    default:
                    case CubeMapFace.PositiveX:
                        // Left
                        Dir = new Vector3(1, 0, 0);
                        VUP = new Vector3(0, 1, 0);
                        color = Color.Black;
                        break;

                    case CubeMapFace.NegativeX:
                        // Right
                        Dir = new Vector3(-1, 0, 0);
                        VUP = new Vector3(0, 1, 0);
                        color = Color.Red;
                        break;

                    case CubeMapFace.PositiveY:
                        // Up
                        Dir = new Vector3(0, 1, 0);
                        VUP = new Vector3(0, 0, -1);
                        color = Color.Gray;
                        break;

                    case CubeMapFace.NegativeY:
                        // Down
                        Dir = new Vector3(0, -1, 0);
                        VUP = new Vector3(0, 0, 1);
                        color = Color.Yellow;
                        break;

                    case CubeMapFace.PositiveZ:
                        // Front
                        Dir = new Vector3(0, 0, 1);
                        VUP = new Vector3(0, 1, 0);
                        color = Color.Green;
                        break;

                    case CubeMapFace.NegativeZ:
                        // Back
                        Dir = new Vector3(0, 0, -1);
                        VUP = new Vector3(0, 1, 0);
                        color = Color.Blue;
                        break;
                }

                var Pos = piso.Position;
                if (nFace == CubeMapFace.NegativeY)
                    Pos.Y += 2000;

                D3DDevice.Instance.Device.Transform.View = Matrix.LookAtLH(Pos, Pos + Dir, VUP);
                D3DDevice.Instance.Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, color, 1.0f, 0);
                D3DDevice.Instance.Device.BeginScene();
                //Renderizar: solo algunas cosas:
                if (nFace == CubeMapFace.NegativeY)
                {
                    //Renderizar terreno                    
                    terreno.render();
                }
                else
                {
                    //Renderizar SkyBox
                    skyBox.render();
                    // dibujo el bosque
                    foreach (var mesh in meshes)
                    {
                        if (FastMath.Pow2(piso.Position.X - mesh.Position.X) +
                            FastMath.Pow2(piso.Position.Z - mesh.Position.Z)
                            < FastMath.Pow2(400) && mesh.Position != piso.Position)
                        {
                            
                            mesh.Effect = envmap;
                            mesh.Technique = "RenderScene";
                            mesh.render();
                        }
                    }
                }
                var fname = string.Format("face{0:D}.bmp", nFace);
                //SurfaceLoader.Save(fname, ImageFileFormat.Bmp, pFace);

                D3DDevice.Instance.Device.EndScene();
            }
            // restuaro el render target
            D3DDevice.Instance.Device.SetRenderTarget(0, pOldRT);
        }

        public void endRenderLagos(TgcCamera camara, TgcFrustum frustum)
        {
            if (RenderUtils.estaDentroDelFrustum(piso, frustum))
            {
                var aspectRatio = D3DDevice.Instance.AspectRatio;
                var g_LightPos = new Vector3(4000f , 6000f, 3000f);
                var lookat = new Vector3(0, 0, 0);
                var g_LightDir = lookat - g_LightPos;
                g_LightDir.Normalize();
    
                //D3DDevice.Instance.Device.Transform.View = camara.GetViewMatrix();
                // FIXME! esto no se bien para que lo hace aca.
                var g_mShadowProj = Matrix.PerspectiveFovLH(Geometry.DegreeToRadian(130.0f), 1f,
                    1f, 100000f);

                 D3DDevice.Instance.Device.Transform.Projection =
                       Matrix.PerspectiveFovLH(Geometry.DegreeToRadian(45.0f), aspectRatio,
                            1f, 100000f);
                // Cargo las var. del shader:
                //envmap.SetValue("g_txCubeMap", g_pCubeMap);
                envmap.SetValue("fvLightPosition", new Vector4(4000f, 8000f, 3000f, 0f));
                envmap.SetValue("fvEyePosition", TgcParserUtils.vector3ToFloat3Array(camara.Position));

                //Doy posicion a la luz
                // Calculo la matriz de view de la luz
                envmap.SetValue("g_vLightPos", new Vector4(g_LightPos.X, g_LightPos.Y, g_LightPos.Z, 1));
                envmap.SetValue("g_vLightDir", new Vector4(g_LightDir.X, g_LightDir.Y, g_LightDir.Z, 1));
                var g_LightView = Matrix.LookAtLH(g_LightPos, g_LightPos + g_LightDir, new Vector3(0, 0, 1));

                // inicializacion standard:
                //envmap.SetValue("g_mProjLight", g_mShadowProj);
                //envmap.SetValue("g_mViewLightProj", g_LightView * g_mShadowProj);


                //D3DDevice.Instance.Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
                piso.AlphaBlendEnable = true;
                // Ahora dibujo el agua
                D3DDevice.Instance.Device.RenderState.AlphaBlendEnable = true;
                envmap.SetValue("aux_Tex", terreno.terrainTexture);
                // posicion de la canoa (divido por la escala)
                //envmap.SetValue("canoa_x", canoa.Position.X / 10.0f);
                //envmap.SetValue("canoa_y", canoa.Position.Z / 10.0f);
                envmap.SetValue("time", time);
                piso.Effect = envmap;
                piso.Technique = "RenderAgua";
                         
                piso.render();
            }
        }

        public void applyShadowMap()
        {
            piso.Effect = envmap;
            //piso.Technique = envmap;
        }

        public void renderWorld(TgcFrustum frustum)
        {
            RenderUtils.renderFromFrustum(meshes, frustum);
        }

        public void renderAll(TgcFrustum Frustum, float ElapsedTime)
        {
            //envmap.Technique = "RenderSceneShadows";
            //terreno.executeRender(envmap);
            //terreno.render();
            RenderUtils.renderFromFrustum(meshes, Frustum);
            RenderUtils.renderFromFrustum(BarrilesExplosivos, Frustum, ElapsedTime);
            envmap.Technique = "RenderScene";
        }


        public void renderShadowMap(TgcFrustum frustum, Effect shadowMap)
        {         
            foreach (var mesh in palmeras)
            {
                mesh.Effect = shadowMap;
                mesh.Technique = "RenderSceneShadows";
            }

            RenderUtils.renderFromFrustum(palmeras, frustum);
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

        public List<Barril> BarrilesExplosivos
        {
            get { return barrilesExplosivos; }
        }
    }
}
