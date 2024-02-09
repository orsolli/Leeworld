#include <iostream>
#include "engine.hpp"

/**
 * @brief Test mutateBlock() by chomping away at a block
 *
 * @return int
 */
int test()
{
    Engine engine;
    int i = 0;
    i++;
    if (engine.mutateBlock(0, 0, 0, 0, true) != "01")
    {
        std::cout << "Test " << i << " failed" << std::endl;
        std::cout << "Output: " << std::endl
                  << engine.getBlock(0, 0, 0) << std::endl;
        return 1;
    }
    i++;
    if (engine.mutateBlock(0, 0, 0, 1, false) != "10001010101010101")
    {
        std::cout << "Test " << i << " failed" << std::endl;
        std::cout << "Output: " << std::endl
                  << engine.getBlock(0, 0, 0) << std::endl;
        return 1;
    }
    i++;
    if (engine.mutateBlock(4.0, 0, 0, 1, false) != "10000010101010101")
    {
        std::cout << "Test " << i << " failed" << std::endl;
        std::cout << "Output: " << std::endl
                  << engine.getBlock(0, 0, 0) << std::endl;
        return 1;
    }
    i++;
    if (engine.mutateBlock(0, 4.0, 0, 1, false) != "10000000101010101")
    {
        std::cout << "Test " << i << " failed" << std::endl;
        std::cout << "Output: " << std::endl
                  << engine.getBlock(0, 0, 0) << std::endl;
        return 1;
    }
    i++;
    if (engine.mutateBlock(6.0, 4.0, 0, 2, false) != "10000001010001010101010101010101")
    {
        std::cout << "Test " << i << " failed" << std::endl;
        std::cout << "Output: " << std::endl
                  << engine.getBlock(0, 0, 0) << std::endl;
        return 1;
    }
    i++;
    if (engine.mutateBlock(6.0, 6.0, 0, 2, false) != "10000001010001000101010101010101")
    {
        std::cout << "Test " << i << " failed" << std::endl;
        std::cout << "Output: " << std::endl
                  << engine.getBlock(0, 0, 0) << std::endl;
        return 1;
    }
    i++;
    if (engine.mutateBlock(6.0, 6.0, 0, 2, true) != "10000001010001010101010101010101")
    {
        std::cout << "Test " << i << " failed" << std::endl;
        std::cout << "Output: " << std::endl
                  << engine.getBlock(0, 0, 0) << std::endl;
        return 1;
    }
    i++;
    if (engine.mutateBlock(6.0, 4.0, 0, 2, true) != "10000000101010101")
    {
        std::cout << "Test " << i << " failed" << std::endl;
        std::cout << "Output: " << std::endl
                  << engine.getBlock(0, 0, 0) << std::endl;
        return 1;
    }
    i++;
    if (engine.mutateBlock(4.0, 4.0, 0, 1, false) != "10000000001010101")
    {
        std::cout << "Test " << i << " failed" << std::endl;
        std::cout << "Output: " << std::endl
                  << engine.getBlock(0, 0, 0) << std::endl;
        return 1;
    }
    i++;
    if (engine.mutateBlock(0, 0, 4.0, 1, false) != "10000000000010101")
    {
        std::cout << "Test " << i << " failed" << std::endl;
        std::cout << "Output: " << std::endl
                  << engine.getBlock(0, 0, 0) << std::endl;
        return 1;
    }
    i++;
    if (engine.mutateBlock(4.0, 0, 4.0, 1, false) != "10000000000000101")
    {
        std::cout << "Test " << i << " failed" << std::endl;
        std::cout << "Output: " << std::endl
                  << engine.getBlock(0, 0, 0) << std::endl;
        return 1;
    }
    i++;
    if (engine.mutateBlock(0, 4.0, 4.0, 1, false) != "10000000000000001")
    {
        std::cout << "Test " << i << " failed" << std::endl;
        std::cout << "Output: " << std::endl
                  << engine.getBlock(0, 0, 0) << std::endl;
        return 1;
    }
    i++;
    if (engine.mutateBlock(4.0, 4.0, 4.0, 1, false) != "00")
    {
        std::cout << "Test " << i << " failed" << std::endl;
        std::cout << "Output: " << std::endl
                  << engine.getBlock(0, 0, 0) << std::endl;
        return 1;
    }
    return 0;
}
