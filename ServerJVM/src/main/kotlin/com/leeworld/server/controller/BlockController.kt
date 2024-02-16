package com.leeworld.server.controller

import org.springframework.web.bind.annotation.GetMapping
import org.springframework.web.bind.annotation.RestController
import org.springframework.web.bind.annotation.RequestMapping
import org.springframework.web.bind.annotation.PathVariable
import org.springframework.web.bind.annotation.PutMapping
import org.springframework.web.bind.annotation.RequestBody
import com.leeworld.server.service.BlockService

@RestController
@RequestMapping("/digg")
class BlockController(private val blockService: BlockService) {

    @GetMapping("/block/{x}/{y}/{z}/{detail}")
    fun GetBlock(
        @PathVariable x: Int,
        @PathVariable y: Int,
        @PathVariable z: Int,
        @PathVariable detail: Int
    ): String {
        val block = blockService.GetBlock(x, y, z, detail).map { "${it.toString(2).padStart(16, '0')}" }
        return block.reduce { result: String?, item: String -> if (result == null) "${item}" else "${result}${item}" }
    }

    @GetMapping("/block/{x}/{y}/{z}/")
    fun GetBlock(
        @PathVariable x: Int,
        @PathVariable y: Int,
        @PathVariable z: Int,
    ): String {
        val block = blockService.GetBlock(x, y, z).map { "${it.toString(2).padStart(16, '0')}" }
        return block.reduce { result: String?, item: String -> if (result == null) "${item}" else "${result}${item}" }
    }

    @PutMapping("/block/{x}/{y}/{z}/{value}")
    fun MutateBlock(
        @PathVariable x: Int,
        @PathVariable y: Int,
        @PathVariable z: Int,
        @PathVariable value: Int,
        @RequestBody path: List<Int>,
    ): String {
        blockService.MutateBlock(x, y, z, path, value == 1)
        val block = blockService.GetBlock(x, y, z, path.size).map { "${it.toString(2).padStart(16, '0')}" }
        return block.reduce { result: String?, item: String -> if (result == null) "${item}" else "${result}${item}" }
    }
}
