# FastHorse-For-SQL
🐎Fast Horse For MSSQL，是一个MSSQL的SQL脚本批量执行的工具：
1.配置好数据库信息后，根目录会生成一个config.dat文件
2.打开需要执行的SQL脚本的文件夹，程序会遍历文件夹中的所有.sql文件，根据文件夹结构文件名称排序依次执行，sql脚本命名请注意！
3.点击运行按钮，会依次按照读取的顺序执行SQL脚本
4.执行完成后根目录会有logs文件夹记录日志，执行失败的脚本会记录在根目录的Failed文件夹，方便后续执行
文件夹结构示例：
----
    |
    |---1.扩展脚本文件夹
    |    |---alter.sql
    |    |---create.sql
    |    |---insert.sql
    |    |---……
    |---2.存储过程文件夹
    |    |---xxxxx.sql
    |    |---xxxxxxx.sql
    |    |---……
    |---3.……
说明
![image](https://github.com/user-attachments/assets/934e00e1-5848-4fa3-a086-5a2973935f3b)
![image](https://github.com/user-attachments/assets/cc45859b-33f9-46b0-bf42-3d586e80a00d)

