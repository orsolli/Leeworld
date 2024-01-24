#include <iostream>
#include "engine.hpp"
#include "test.hpp"
#include <string>
#include <string.h>
#include <vector>
#include <sstream>

std::vector<std::string> split(const std::string &s, char delimiter)
{
    std::vector<std::string> tokens;
    std::string token;
    std::istringstream tokenStream(s);
    while (std::getline(tokenStream, token, delimiter))
    {
        tokens.push_back(token);
    }
    return tokens;
}

int main(int argc, char *argv[])
{
    //  --mutate {octree} --level {level} --position {position} --build
    // ./Engine/build/LeeworldEngine --mutate 01 --level 3 --position 0.5,0.2,0.75
    bool interactive = true;
    std::string octree;
    int level;
    float positionX;
    float positionY;
    float positionZ;
    bool build;
    for (int i = 0; i < argc; i++)
    {
        if (strcmp("--mutate", argv[i]) == 0)
        {
            interactive = false;
            octree = argv[++i];
        }
        if (strcmp("--level", argv[i]) == 0)
        {
            level = std::stoi(argv[++i]);
        }
        if (strcmp("--position", argv[i]) == 0)
        {
            std::vector<std::string> position = split(argv[++i], ',');
            positionX = std::stof(position[0]);
            positionY = std::stof(position[1]);
            positionZ = std::stof(position[2]);
        }
        if (strcmp("--build", argv[i]) == 0)
        {
            build = true;
        }
    }
    if (!interactive)
    {
        Engine engine(octree);
        std::cout << engine.mutateBlock(positionX, positionY, positionZ, level, build) << std::endl;
        return 0;
    }
    Engine engine;
    while (true)
    {
        std::cout << "0: Quit" << std::endl;
        std::cout << "1: Read" << std::endl;
        std::cout << "2: Write" << std::endl;
        std::cout << "3: Test" << std::endl;
        int i;
        std::cin >> i;
        if (i == 0)
        {
            break;
        }
        else if (i == 1)
        {
            int x, y, z;
            std::cout << "Position along x-axis?" << std::endl;
            std::cin >> x;
            std::cout << "Position along y-axis?" << std::endl;
            std::cin >> y;
            std::cout << "Position along z-axis?" << std::endl;
            std::cin >> z;
            std::cout << std::endl
                      << engine.getBlock(x, y, z) << std::endl;
        }
        else if (i == 2)
        {
            int x, y, z, level, value;
            std::cout << "Do you want to remove(0) or add (1) a block?" << std::endl;
            std::cin >> value;
            std::cout << "At what level do you want to set a value? (1=big, 6=small)" << std::endl;
            std::cin >> level;
            std::cout << "Position along x-axis?" << std::endl;
            std::cin >> x;
            std::cout << "Position along y-axis?" << std::endl;
            std::cin >> y;
            std::cout << "Position along z-axis?" << std::endl;
            std::cin >> z;
            std::cout << std::endl
                      << engine.mutateBlock(x, y, z, level, value == 1) << std::endl;
        }
        else if (i == 3)
        {
            if (test() == 0)
            {
                std::cout << "All tests passed" << std::endl;
            }
            else
            {
                std::cout << "Some tests failed" << std::endl;
            };
        }
        else
        {
            std::cout << "Invalid input" << std::endl;
        }
        std::cout << std::endl;
    }
    return 0;
}
