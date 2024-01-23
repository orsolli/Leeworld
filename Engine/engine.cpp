#include <string>
#include "engine.hpp"
#include <vector>
#include <cmath>

std::vector<std::vector<int>> direction = {
    {0, 0, 0},
    {1, 0, 0},
    {0, 1, 0},
    {1, 1, 0},
    {0, 0, 1},
    {1, 0, 1},
    {0, 1, 1},
    {1, 1, 1} // This order is "increment x first"
};

std::string toOctree(const std::vector<int> path, std::string existingValue, bool isInside)
{
    std::string cellValue = isInside ? "1" : "0";
    if (path.empty())
        return "0" + cellValue;
    std::string octree = "0000000000000000";

    for (int i = 0; i < 8; i++)
    {
        octree[i * 2 + 1] = i + 1 == path.back() ? cellValue[0] : existingValue[0];
    }
    for (int j = path.size() - 2; j >= 0; j--)
    {
        std::string subtree = "";
        for (int i = 0; i < 8; i++)
        {
            if (i + 1 == path[j])
                subtree += "1" + octree;
            else
                subtree += "0" + existingValue;
        }
        octree = subtree;
    }

    return "1" + octree;
};
bool isBefore(std::vector<std::pair<int, int>> self, std::vector<int> goal, int index)
{
    bool isIndexedItemBefore = true;
    if (index < self.size() && index < goal.size())
    {
        isIndexedItemBefore = self[index].first <= goal[index];
    }
    std::vector<int> previousItemsGoal(goal.begin(), goal.begin() + index);
    std::vector<std::pair<int, int>> previousItemsSelf(self.begin(), self.begin() + index);
    bool isPreviousItemsBefore = true;
    if (previousItemsSelf.size() != previousItemsGoal.size())
    {
        isPreviousItemsBefore = false;
    }
    else
    {
        for (int i = 0; i < previousItemsSelf.size(); i++)
        {
            if (previousItemsSelf[i].first != previousItemsGoal[i])
            {
                isPreviousItemsBefore = false;
                break;
            }
        }
    }
    return isIndexedItemBefore && isPreviousItemsBefore;
};

std::string Engine::getBlock(int x, int y, int z)
{
    std::string key = std::to_string(x) + "_" + std::to_string(y) + "_" + std::to_string(z);
    if (chunks.find(key) == chunks.end())
    {
        if (x < 0 || y < 0 || z < 0)
            return "01";
        return "00";
    }
    return chunks[key];
};

std::string Engine::mutateBlock(float x, float y, float z, int level, bool add)
{
    int X = x / 8;
    int Y = y / 8;
    int Z = z / 8;

    std::vector<float> pos({(float)(x - X * 8) / 8 - 1 / std::pow((float)2, (float)(level + 1)),
                            (float)(y - Y * 8) / 8 - 1 / std::pow((float)2, (float)(level + 1)),
                            (float)(z - Z * 8) / 8 - 1 / std::pow((float)2, (float)(level + 1))});
    std::vector<int> path;
    for (int i = 0; i < level; i++)
    {
        int index = 0;
        if (int(pos[0]) > 0)
        {
            index += 1;
            pos[0] -= 1;
        }
        if (int(pos[1]) > 0)
        {
            index += 2;
            pos[1] -= 1;
        }
        if (int(pos[2]) > 0)
        {
            index += 4;
            pos[2] -= 1;
        }
        path.push_back(index + 1);
        pos[0] = pos[0] * 2 - direction[index][0];
        pos[1] = pos[1] * 2 - direction[index][1];
        pos[2] = pos[2] * 2 - direction[index][2];
    }

    std::string octreeString = getBlock(X, Y, Z);
    std::vector<std::pair<int, int>> pathMap = {std::make_pair(1, 0)};
    int i = 1;
    auto startIndex = pathMap[0];

    while (isBefore(pathMap, path, i))
    {
        if (i < path.size() && i < pathMap.size() && pathMap[i].first == path[i])
        {
            startIndex = pathMap[i];
            i++;
        }
        if (octreeString[pathMap.back().second] == '1') // If node is a branch
        {
            pathMap.push_back(std::make_pair(1, pathMap.back().second + 1)); // Go to first child one level deeper
        }
        else if (octreeString[pathMap.back().second] == '0') // If node is a leaf
        {
            auto nextStringIndex = pathMap.back().second + 2;
            if (pathMap.back().first == 8)
                pathMap.pop_back();                                                     // Go to next child in parent
            pathMap.back() = std::make_pair(pathMap.back().first + 1, nextStringIndex); // Increment child-index and string-index
            while (pathMap.back().first > 8)
            {
                pathMap.pop_back();                                                         // Go to next child in parent
                pathMap.back() = std::make_pair(pathMap.back().first + 1, nextStringIndex); // Increment child-index and string-index
            }
        }
        if (pathMap.back().second == octreeString.length())
            break; // Return if end is reached
    }

    auto first_part = octreeString.substr(0, startIndex.second);
    auto last_part = pathMap.size() == 1 ? "" : octreeString.substr(pathMap.back().second);
    std::vector<int> diffPath(path.begin() + i, path.end());
    auto new_part = toOctree(diffPath, octreeString.substr(startIndex.second + 1, 1), add);
    octreeString = first_part + new_part + last_part;

    chunks[std::to_string(X) + "_" + std::to_string(Y) + "_" + std::to_string(Z)] = octreeString;
    return octreeString;
};
