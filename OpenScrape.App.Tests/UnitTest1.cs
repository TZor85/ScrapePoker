namespace OpenScrape.App.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestHacenEscalera()
        {
            // Caso de prueba 1: números que forman una escalera
            List<int> numeros1 = new List<int> { 8, 4, 7 };
            bool resultado1 = hacenEscalera(numeros1);
            Assert.IsTrue(resultado1);

            List<int> numeros3 = new List<int> { 14, 10, 13 };
            bool resultado3 = hacenEscalera(numeros3);
            Assert.IsTrue(resultado3);

            // Caso de prueba 2: números que no forman una escalera
            List<int> numeros2 = new List<int> { 6, 2, 7 };
            bool resultado2 = hacenEscalera(numeros2);
            Assert.IsFalse(resultado2);
        }


        bool hacenEscalera(List<int> numeros)
        {
            // Ordena la lista de números
            numeros.Sort();

            if (numeros[1] - numeros[0] <= 3)
                return true;

            return false;
        }
    }

}

