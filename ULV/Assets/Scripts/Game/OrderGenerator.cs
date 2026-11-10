using System.Collections.Generic;

/// <summary>Un pedido de un cliente: los sabores de cada piso (de abajo hacia arriba) y su reto.</summary>
public class CakeOrder
{
    public int customer;
    public int[] flavors;               // índice de sabor por piso, [0] = piso de abajo
    public List<int> pool;              // sabores que se usan en este pedido
    public ChallengeType challenge;
    public float challengeValue;        // N capas perfectas o % de ancho
    public float patience;              // segundos (0 = sin límite)
}

/// <summary>
/// GENERACIÓN PROCEDURAL de pedidos. Con la semilla del nivel crea pedidos distintos
/// en cada partida, siguiendo reglas:
///  - usa solo "flavorCount" sabores del nivel (elegidos al azar)
///  - nunca 3 pisos seguidos del mismo sabor
///  - cada pedido usa al menos 2 sabores distintos
///  - los retos son más exigentes en el 2º y 3º pedido
/// </summary>
public static class OrderGenerator
{
    public static CakeOrder Create(System.Random rng, LevelCatalog.Entry level, int totalFlavors,
                                   int totalCustomers, int orderIndex, int lastCustomer)
    {
        CakeOrder o = new CakeOrder();

        // Cliente (no repetir el anterior)
        o.customer = totalCustomers <= 1 ? 0 : rng.Next(totalCustomers);
        if (totalCustomers > 1 && o.customer == lastCustomer) o.customer = (o.customer + 1) % totalCustomers;

        // Sabores disponibles en este pedido
        int count = UnityEngine.Mathf.Clamp(level.flavorCount, 2, totalFlavors);
        List<int> all = new List<int>();
        for (int i = 0; i < totalFlavors; i++) all.Add(i);
        o.pool = new List<int>();
        for (int i = 0; i < count; i++)
        {
            int pick = rng.Next(all.Count);
            o.pool.Add(all[pick]);
            all.RemoveAt(pick);
        }

        // Pisos
        int floors = level.floorsPerOrder;
        o.flavors = new int[floors];
        for (int f = 0; f < floors; f++)
        {
            int flavor;
            int guard = 0;
            do
            {
                flavor = o.pool[rng.Next(o.pool.Count)];
                guard++;
            } while (guard < 20 && f >= 2 && flavor == o.flavors[f - 1] && flavor == o.flavors[f - 2]);
            o.flavors[f] = flavor;
        }
        // Al menos 2 sabores distintos
        bool allSame = true;
        for (int f = 1; f < floors; f++) if (o.flavors[f] != o.flavors[0]) { allSame = false; break; }
        if (allSame && floors > 1) o.flavors[floors / 2] = o.pool[(o.pool.IndexOf(o.flavors[0]) + 1) % o.pool.Count];

        // Reto
        o.challenge = ChallengeType.None;
        if (level.challenges != null && level.challenges.Length > 0 && rng.NextDouble() < level.challengeChance)
        {
            o.challenge = level.challenges[rng.Next(level.challenges.Length)];
            switch (o.challenge)
            {
                case ChallengeType.PerfectDrops:
                    o.challengeValue = UnityEngine.Mathf.Min(floors - 1, 2 + orderIndex + rng.Next(2));
                    break;
                case ChallengeType.MinFinalWidth:
                    o.challengeValue = 0.4f + 0.1f * orderIndex + (float)rng.NextDouble() * 0.1f;
                    break;
            }
        }

        // Paciencia (con un poco de variación)
        o.patience = level.patienceSeconds > 0f
            ? level.patienceSeconds * (0.9f + (float)rng.NextDouble() * 0.2f)
            : 0f;
        return o;
    }

    /// <summary>Un sabor que NO aparece en ningún piso del pedido (-1 si no hay).</summary>
    public static int RandomFlavorNotInOrder(System.Random rng, CakeOrder o, int totalFlavors)
    {
        List<int> options = new List<int>();
        for (int f = 0; f < totalFlavors; f++)
            if (System.Array.IndexOf(o.flavors, f) < 0) options.Add(f);
        return options.Count == 0 ? -1 : options[rng.Next(options.Count)];
    }

    /// <summary>
    /// El cliente cambia de opinión: cambia el sabor de un piso que todavía no se ha hecho.
    /// Devuelve el índice del piso cambiado (o -1).
    /// </summary>
    public static int ChangeFloor(System.Random rng, CakeOrder o, int firstFreeFloor)
    {
        if (firstFreeFloor >= o.flavors.Length || o.pool.Count < 2) return -1;
        int j = rng.Next(firstFreeFloor, o.flavors.Length);
        int old = o.flavors[j];
        int guard = 0, flavor;
        do { flavor = o.pool[rng.Next(o.pool.Count)]; guard++; } while (flavor == old && guard < 20);
        if (flavor == old) return -1;
        o.flavors[j] = flavor;
        return j;
    }
}
