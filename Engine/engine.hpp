#include <unordered_map>
#include <string>

class Engine
{
protected:
    std::unordered_map<std::string, std::string> chunks = {};
    /*
        {"0_0_0", "01"},
        {"2_0_0", "10110000010000000000010001000100"},
        {"2_-1_0", "10110000010000000000010001000100"},
        {"0_2_1", "10101011010101000101010101011010101010101000101"},
        {"1_0_0", "10000001000010000010000000000000000000000000000"},
    };*/

public:
    std::string getBlock(int x, int y, int z);
    std::string mutateBlock(float x, float y, float z, int level, bool add);
};
