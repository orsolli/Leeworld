package com.leeworld.server.service

import java.util.function.Consumer
import org.springframework.stereotype.Service
import com.leeworld.server.datasource.BlockDataSource
import com.leeworld.server.service.isLeaf
import kotlin.collections.remove

@Service
class BlockService(private val dataSource: BlockDataSource) {
    fun GetBlock(x: Int, y: Int, z: Int, detail: Int = 0): ArrayList<Short> {
        val octreeStream = dataSource.getBlock(x, y, z)
        var nNodesInThisLevel = 1
        var nNodesInNextLevel = 0
        var level = 0
        val result = ArrayList<Short>()
        val stream = octreeStream.peek({ node: Short ->
            result.add(node)
            for (power in 1..<16 step 2) {
                if (1.shl(power).and(node.toInt()) > 0) {
                    nNodesInNextLevel++
                }
            }
            nNodesInThisLevel--;
            if (nNodesInThisLevel == 0) {
                nNodesInThisLevel = nNodesInNextLevel
                nNodesInNextLevel = 0
                level++
            }
        }).takeWhile { level != detail || level == 0 }
        val liste = stream.toList()
        println(liste)
        stream.close()
        return result
    }

    fun MutateBlock(x: Int, y: Int, z: Int, path: List<Int>, value: Boolean) {
        val octreeStream = dataSource.getBlock(x, y, z)
        dataSource.setBlock(x, y, z, Mutate(octreeStream.toList().toMutableList(), path, value))
    }
}

fun Mutate(octree: List<Short>, path: List<Int>, value: Boolean): MutableList<Short> {
    val newOctree = octree.toMutableList()
    // Analyze
    val levelIndexes = mutableListOf<Int>(0, 1)
    var index = 0
    for (level in 1..<path.size) {
        var indexOfThisLevel = levelIndexes[level]
        var n = 0
        do
            n += numberOfNodesBefore(newOctree[index++])
        while (indexOfThisLevel > index && index < newOctree.size)

        if (index >= newOctree.size - 1) break
        levelIndexes.add(indexOfThisLevel + n)
    }

    // Add more detail
    for (i in levelIndexes.size..<path.size) {
        val parentIndex = levelIndexes[i-1] + numberOfNodesBefore(newOctree[levelIndexes[i-1]], path[i-1])
        val parentValue = newOctree.get(parentIndex)
        val parentIsSet = isSet(parentValue, path[i-1])
        if (!isLeaf(parentValue, path[i])) throw Exception("Cropped octree")
        levelIndexes.add(newOctree.size)
        newOctree.add(levelIndexes[i], if (parentIsSet) 0b0101_0101_0101_0101.toShort() else 0b0000_0000_0000_0000.toShort())
    }

    // Mutate
    val currentIndex = levelIndexes.last() + numberOfNodesBefore(newOctree[levelIndexes.last()], path.last())
    val currentValue = newOctree.get(currentIndex)
    if (isSet(currentValue, path.last()) == value) return octree.toMutableList()
    var flipper = 1.shl((7-path.last())*2)
    val newValue = if (value) flipper.or(currentValue.toInt()) else flipper.inv().and(currentValue.toInt())
    newOctree.set(currentIndex, newValue.toShort())

    // Backpropagate
    for (p in path.size-2 downTo 0) {
        val ancestorIndex = levelIndexes[p] + numberOfNodesBefore(newOctree[levelIndexes[p]], path[p])
        val ancestorValue = newOctree.get(ancestorIndex)
        val ancestorIsSet = isSet(ancestorValue, path[p])

        val childIndex = levelIndexes[p+1] + numberOfNodesBefore(newOctree[levelIndexes[p+1]], path[p+1])
        val childValue = newOctree.get(childIndex)
        val childIsSet = isSet(childValue, path[p+1])

        val nSetInAncestor = numberOfSet(ancestorValue)

        flipper = 1.shl((7-path[p])*2)
        var newAncestor = if (childIsSet) flipper.or(ancestorValue.toInt()).toShort() else flipper.inv().and(ancestorValue.toInt()).toShort()

        // Remove detail
        if (childValue == 0b0000_0000_0000_0000.toShort() || childValue == 0b0101_0101_0101_0101.toShort()) {
            newOctree.removeAt(childIndex)
            flipper =  2.shl((7-path[p])*2).inv()
            newAncestor = flipper.and(ancestorValue.toInt()).toShort()
        }

        newOctree.set(ancestorIndex, newAncestor)
        if (ancestorIsSet != childIsSet && ((nSetInAncestor == 4 && !childIsSet) || (nSetInAncestor == 3 && childIsSet))) continue
        break
    }

    return newOctree
}

fun numberOfNodesBefore(node: Short, index: Int = 8): Int {
    var mask = 2 shl (8-index)*2 // 0000_0000_0000_0000_0000_0000_0000_0010
    mask = mask - 1 // 0000_0000_0000_0000_0000_0000_0000_0001
    mask = mask.inv() // 1111_1111_1111_1111_1111_1111_1111_1110
    mask = mask.and(0b1010_1010_1010_1010) // 0000_0000_0000_0000_1010_1010_1010_1010
    var result = mask.and(node.toInt()).toString(2).filter { it == '1' }
    return result.count()
}
fun isLeaf(node: Short, index: Int): Boolean {
    var mask = 2 shl (7-index)*2 // 0000_0000_0000_0000_0000_0000_0000_0010
    var result = mask.and(node.toInt())
    return result == 0
}
fun isSet(node: Short, index: Int): Boolean {
    var mask = 1 shl (7-index)*2 // 0000_0000_0000_0000_0000_0000_0000_0001
    var result = mask.and(node.toInt())
    return result != 0
}

fun numberOfSet(node: Short): Int {
    var mask = 0.inv() // 1111_1111_1111_1111_1111_1111_1111_1111
    mask = mask.and(0b0101_0101_0101_0101) // 0000_0000_0000_0000_0101_0101_0101_0101
    var result = mask.and(node.toInt()).toString(2).filter { it == '1' }
    return result.count()
}

