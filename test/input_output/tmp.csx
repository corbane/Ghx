
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{

    static void Main(string[] args)
    {
        var pool = new List<Task<ulong>>();
        foreach(var value in Enumerable.Range(1,5))
        {
            pool.Add(Task.Run(new Func<ulong>(()=>
            {
                ulong somme = 1;
                foreach (var index in Enumerable.Range(1, value))
                    somme *= (ulong)index;
                
                return somme;
            })));                
        }
        Task.WaitAll(pool.ToArray());
        foreach (var value in pool)
        {
            Console.WriteLine(value.Result);
        }
        Console.ReadKey();
    }      
}
