using System;
using System.Collections.Generic;

[Serializable]
class Blueprint
{
    public string player;
    public string block;
    public string position;
    public int size_x;
    public int size_y;
    public int size_z;
}

[Serializable]
class BlueprintList
{
    public List<Blueprint> blueprints;
}