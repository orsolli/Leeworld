package com.leeworld.server.configuration

import org.springframework.context.annotation.Configuration
import org.springframework.context.annotation.Profile
import org.springframework.context.annotation.Bean
import org.springframework.data.redis.connection.jedis.JedisConnectionFactory
import org.springframework.data.redis.connection.jedis.JedisClientConfiguration
import org.springframework.data.redis.connection.RedisStandaloneConfiguration
import org.springframework.data.redis.core.RedisTemplate
import org.springframework.data.redis.serializer.GenericToStringSerializer
import org.springframework.data.redis.serializer.JdkSerializationRedisSerializer
import org.springframework.data.redis.serializer.RedisSerializer
import org.springframework.beans.factory.annotation.Value
import java.nio.ByteBuffer
import org.slf4j.LoggerFactory

@Configuration
@Profile("redis")
class RedisConfig {

    @Value("\${spring.data.redis.host}")
    lateinit var redisHost: String

    @Value("\${spring.data.redis.port}")
    lateinit var redisPort: String

    val logger = LoggerFactory.getLogger(RedisConfig::class.java)

    @Bean
    fun jedisConnectionFactory(): JedisConnectionFactory {
        val config = RedisStandaloneConfiguration(redisHost, redisPort.toInt())
        logger.info("Using Redis at $redisHost:$redisPort")
        val jedisClientConfiguration = JedisClientConfiguration.builder().usePooling().build()
        val factory = JedisConnectionFactory(config, jedisClientConfiguration)
        factory.afterPropertiesSet()
        return factory
    }

    @Bean
    fun redisTemplate(): RedisTemplate<String, List<UShort>> {
        val template = RedisTemplate<String, List<UShort>>()
        template.setConnectionFactory(jedisConnectionFactory())
        template.setValueSerializer(UShortListToByteRedisSerializer())
        return template
    }
}

class UShortListToByteRedisSerializer : RedisSerializer<List<UShort>> {

    override fun serialize(data: List<UShort>?): ByteArray? {
        return data?.let {
            ByteBuffer.allocate(data.size * java.lang.Short.BYTES)
                .apply { data.forEach { putShort(it.toShort()) } }
                .array()
        }
    }

    override fun deserialize(bytes: ByteArray?): List<UShort>? {
        return bytes?.let {
            val buffer = ByteBuffer.wrap(bytes)
            val list = mutableListOf<UShort>()

            while (buffer.hasRemaining()) {
                list.add(buffer.short.toUShort())
            }

            list
        }
    }
}