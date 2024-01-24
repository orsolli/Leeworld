#include <iostream>
#include "engine.hpp"
#include "test.hpp"

int main()
{
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
