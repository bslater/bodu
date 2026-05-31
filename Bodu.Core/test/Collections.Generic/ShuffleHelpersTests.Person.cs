// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShuffleHelpersTests.Person.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ShuffleHelpersTests
{

    /// <summary>
    /// Reference-type sample used to verify that shuffle operations preserve element identity
    /// (not just value equality) when shuffling sequences of reference-type items.
    /// </summary>
    public class Person
    {

        public Person(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; }

        public override bool Equals(object? obj) => obj is Person other && Id == other.Id;

        public override int GetHashCode() => Id.GetHashCode();

    }

}
